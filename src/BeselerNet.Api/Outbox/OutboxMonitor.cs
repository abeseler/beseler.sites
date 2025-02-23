using BeselerNet.Api.Core;
using BeselerNet.Shared.Core;
using System.Text.Json;

namespace BeselerNet.Api.Outbox;

internal sealed class OutboxMonitor(OutboxDataSource dataSource, DomainEventHandler domainEventHandler, ILogger<OutboxMonitor> logger) : BackgroundService
{
    private const int DEQUEUE_MSG_LIMIT = 10;
    private const int MAX_DELAY_SECONDS = 120;
    private readonly OutboxDataSource _dataSource = dataSource;
    private readonly DomainEventHandler _domainEventHandler = domainEventHandler;
    private readonly ILogger<OutboxMonitor> _logger = logger;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxMonitor has started");

        var backoffPow = 0;
        var delay = TimeSpan.FromSeconds(0);
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(delay, stoppingToken);

            try
            {
                var messages = await _dataSource.Dequeue(DEQUEUE_MSG_LIMIT, stoppingToken);
                if (messages.Length == 0)
                {
                    backoffPow += 1;
                    delay = TimeSpan.FromSeconds(Math.Min(MAX_DELAY_SECONDS, (int)Math.Pow(2, backoffPow)));
                    _logger.LogDebug("No outbox messages to process. Next check in {Seconds} seconds", delay.TotalSeconds);
                    continue;
                }
                
                _logger.LogInformation("Received {Count} messages from the outbox to process", messages.Length);

                await foreach (var task in Task.WhenEach(messages.Select(Process)))
                {
                    if (task.Result.Succeeded(out var message))
                    {
                        await _dataSource.Delete(message.MessageId, CancellationToken.None);
                    }
                }

                backoffPow = 0;
            }
            catch (Exception ex)
            {
                backoffPow += 1;
                delay = TimeSpan.FromSeconds(Math.Min(MAX_DELAY_SECONDS, (int)Math.Pow(2, backoffPow)));
                _logger.LogError(ex, "Error processing outbox messages. Next attempt in {Seconds} seconds", delay.TotalSeconds);
            }
        }

        _logger.LogInformation("OutboxMonitor has stopped");
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
