using BeselerNet.Api.Core;
using BeselerNet.Shared.Core;
using System.Text.Json;

namespace BeselerNet.Api.Outbox;

internal sealed class OutboxMessageProcessor(IServiceScopeFactory scopeFactory, DomainEventDispatcher domainEventDispatcher, ILogger<OutboxMessageProcessor> logger)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly DomainEventDispatcher _domainEventDispatcher = domainEventDispatcher;
    private readonly ILogger<OutboxMessageProcessor> _logger = logger;

    public async Task<Result<OutboxMessage>> Process(OutboxMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing outbox message {MessageId} of type {MessageType}", message.MessageId, message.MessageType);

        using var scope = _scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<OutboxDataSource>();
        try
        {
            if (message.MessageType == nameof(DomainEventMessage))
            {
                var domainEventMessage = JsonSerializer.Deserialize<DomainEventMessage>(message.MessageData, JsonSerializerOptions.Web)
                    ?? throw new InvalidOperationException("Failed to deserialize domain event message from outbox.");

                await _domainEventDispatcher.Dispatch(domainEventMessage, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Unknown message type {MessageType} for outbox message {MessageId}", message.MessageType, message.MessageId);
            }

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing outbox message {MessageId}", message.MessageId);
            return ex;
        }
    }
}
