using BeselerNet.Api.Core;
using BeselerNet.Shared.Core;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BeselerNet.Api.Outbox;

internal sealed class OutboxMonitor(OutboxDataSource dataSource, DomainEventHandler domainEventHandler, ILogger<OutboxMonitor> logger, IOptions<FeaturesOptions> features) : BackgroundService
{
    private const int MAX_MESSAGES = 5;
    private readonly OutboxDataSource _dataSource = dataSource;
    private readonly DomainEventHandler _domainEventHandler = domainEventHandler;
    private readonly ILogger<OutboxMonitor> _logger = logger;
    private readonly FeaturesOptions _features = features.Value;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMilliseconds(500));
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_features.OutboxEnabled)
        {
            _logger.LogWarning("Outbox is disabled.");
            return;
        }

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
    }

    private async Task<Result<OutboxMessage>> Process(OutboxMessage message)
    {
        try
        {
            if (message.MessageType is nameof(DomainEvent))
            {
                var domainEvent = JsonSerializer.Deserialize<DomainEvent>(message.MessageData, JsonSerializerOptions.Web)
                    ?? throw new InvalidOperationException("Failed to deserialize domain event.");
                
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
