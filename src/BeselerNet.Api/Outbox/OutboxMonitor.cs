using BeselerNet.Api.Core;
using BeselerNet.Shared.Core;
using System.Text.Json;

namespace BeselerNet.Api.Outbox;

internal sealed class OutboxMonitor(OutboxDataSource dataSource, DomainEventHandler domainEventHandler, ILogger<OutboxMonitor> logger) : BackgroundService
{
    private const int MAX_MESSAGES = 5;
    private readonly OutboxDataSource _dataSource = dataSource;
    private readonly DomainEventHandler _domainEventHandler = domainEventHandler;
    private readonly ILogger<OutboxMonitor> _logger = logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(5));
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxMonitor has started");

        var backoffPow = 0;
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var messages = new List<OutboxMessage>();
                while (messages.Count < MAX_MESSAGES && await _dataSource.Dequeue(stoppingToken) is { } message)
                {
                    messages.Add(message);
                }

                await foreach (var task in Task.WhenEach(messages.Select(Process)))
                {
                    if (task.Result.Succeeded(out var message))
                    {
                        await _dataSource.Delete(message.MessageId, stoppingToken);
                    }
                }

                backoffPow = 0;
            }
            catch (Exception ex)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(60, (int)Math.Pow(2, backoffPow)));
                backoffPow += 1;
                _logger.LogError(ex, "Error processing outbox. Retrying in {Delay} seconds", delay.TotalSeconds);

                await Task.Delay(delay, stoppingToken);
            }
        }

        if (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("OutboxMonitor has stopped");
        }
        else
        {
            _logger.LogCritical("OutboxMonitor has stopped unexpectedly");
        }
    }

    private async Task<Result<OutboxMessage>> Process(OutboxMessage message)
    {
        _logger.LogDebug("Processing outbox message {MessageId}. {Data}", message.MessageId, message.MessageData);
        
        try
        {
            if (message.MessageType is nameof(DomainEvent))
            {
                var domainEvent = JsonSerializer.Deserialize<DomainEvent>(message.MessageData, JsonSerializerOptions.Web)
                    ?? throw new InvalidOperationException("Failed to deserialize domain event.");
                
                _logger.LogDebug("Outbox message {MessageId} deserialized to: {EventType}", message.MessageId, domainEvent.GetType().Name);

                await _domainEventHandler.Handle(domainEvent, CancellationToken.None);
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
