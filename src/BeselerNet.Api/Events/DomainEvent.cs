using BeselerNet.Api.Events;
using BeselerNet.Api.Outbox;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeselerNet.Api.Core;

internal interface IHandler<T> where T : DomainEvent
{
    Task Handle(T domainEvent, IEventMetadata metadata, CancellationToken cancellationToken);
}

internal interface IEventMetadata
{
    public Guid EventId { get; }
    public DateTimeOffset CreatedAt { get; }
    public string? TraceId { get; }
}

[JsonPolymorphic]
internal abstract partial record DomainEvent;

internal sealed record DomainEventMessage : IEventMetadata
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? TraceId { get; init; } = Activity.Current?.Id ?? Activity.Current?.ParentId;
    public required DomainEvent Payload { get; init; }
    public static DomainEventMessage Wrap(DomainEvent payload) => new() { Payload = payload };
    public EventLogEntity ToEventLog() => new(EventId, Payload.GetType().Name, JsonSerializer.Serialize(Payload, JsonSerializerOptions.Web), CreatedAt);
    public OutboxMessage ToOutboxMessage() => new()
    {
        MessageId = EventId,
        MessageType = nameof(DomainEventMessage),
        MessageData = JsonSerializer.Serialize(this, JsonSerializerOptions.Web),
        InvisibleUntil = DateTime.UtcNow,
        ReceivesRemaining = 5
    };
}

internal sealed class DomainEventDispatcher(IServiceScopeFactory scopeFactory, ILogger<DomainEventDispatcher> logger)
{
    private const string ActivityName = $"{nameof(DomainEventDispatcher)}.{nameof(Dispatch)}";
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<DomainEventDispatcher> _logger = logger;
    private readonly ConcurrentDictionary<string, (Type, MethodInfo)> _cache = new();

    public async Task Dispatch(DomainEventMessage message, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.Source.StartActivity(ActivityName, ActivityKind.Internal, message.TraceId);
        using var scope = _scopeFactory.CreateScope();
        var eventLog = scope.ServiceProvider.GetRequiredService<EventLogDataSource>();

        if (await eventLog.ExistsAsync(message.EventId, cancellationToken))
        {
            return;
        }

        var eventType = message.Payload.GetType();
        var cacheKey = eventType.AssemblyQualifiedName
            ?? throw new InvalidOperationException("Domain event type must have an assembly qualified name.");

        var (handlerType, method) = _cache.GetOrAdd(cacheKey, key =>
        {
            var domainEventType = Type.GetType(key);
            var handlerType = typeof(IHandler<>).MakeGenericType(domainEventType!);
            var method = handlerType.GetMethod(nameof(IHandler<DomainEvent>.Handle))
                ?? throw new InvalidOperationException($"Handler method not found for domain event type '{key}'.");

            return (handlerType, method);
        });

        var handler = scope.ServiceProvider.GetService(handlerType);
        if (handler is null)
        {
            _logger.LogWarning("No handler registered for domain event type '{EventType}'. Skipping.", eventType.Name);
            return;
        }
        
        await (Task)method.Invoke(handler, [message.Payload, message, cancellationToken])!;
        await eventLog.Append(message.ToEventLog(), cancellationToken);
    }
}
