using BeselerNet.Api.Core;
using BeselerNet.Api.Events;
using BeselerNet.Shared.Core;
using System.Text.Json;

namespace BeselerNet.Api.Outbox;

internal sealed class OutboxMonitor(OutboxDataSource dataSource, DomainEventHandler domainEventHandler, ILogger<OutboxMonitor> logger) : BackgroundService
{
    private const int DEQUEUE_MSG_LIMIT = 10;
    private const int MAX_BACKOFF_POW = 7;
    private readonly OutboxDataSource _dataSource = dataSource;
    private readonly DomainEventHandler _domainEventHandler = domainEventHandler;
    private readonly ILogger<OutboxMonitor> _logger = logger;
    private int _backoffPow = 0;
    private TimeSpan Delay => TimeSpan.FromSeconds((int)Math.Pow(2, _backoffPow));
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxMonitor has started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await _dataSource.Dequeue(DEQUEUE_MSG_LIMIT, stoppingToken);
                if (messages.Length == 0)
                {
                    _backoffPow = Math.Min(_backoffPow + 1, MAX_BACKOFF_POW);
                    _logger.LogDebug("No outbox messages to process. Next check in {Seconds} seconds", Delay.TotalSeconds);
                }
                else
                {
                    _logger.LogInformation("Received {Count} messages from the outbox to process", messages.Length);

                    await foreach (var task in Task.WhenEach(messages.Select(Process)))
                    {
                        if (task.Result.Succeeded(out var message))
                        {
                            _ = await _dataSource.Delete(message.MessageId, CancellationToken.None);
                        }
                    }

                    _backoffPow = 0;
                    continue;
                }
            }
            catch (Exception ex)
            {
                _backoffPow = Math.Min(_backoffPow + 1, MAX_BACKOFF_POW);
                _logger.LogError(ex, "Error processing outbox messages. Next attempt in {Seconds} seconds", Delay.TotalSeconds);
            }

            await Task.Delay(Delay, stoppingToken);
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
            else
            {
                _logger.LogWarning("Unknown message type {MessageType} for outbox message {MessageId}", message.MessageType, message.MessageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing outbox message {MessageId}", message.MessageId);
            return ex;
        }

        return message;
    }
}
