using Beseler.ServiceDefaults;
using System.Threading.Channels;

namespace BeselerNet.Api.Outbox;

internal sealed class OutboxMonitor(OutboxDataSource dataSource, OutboxMessageProcessor processor, IAppStartup appStartup, ILogger<OutboxMonitor> logger) : BackgroundService
{
    private const int DEQUEUE_MSG_LIMIT = 10;
    private static readonly Channel<int> _queue = Channel.CreateBounded<int>(new BoundedChannelOptions(1)
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false,
        FullMode = BoundedChannelFullMode.DropWrite
    });
    public static void NotifyMessageAvailable() => _queue.Writer.TryWrite(1);
    private readonly OutboxDataSource _dataSource = dataSource;
    private readonly OutboxMessageProcessor _processor = processor;
    private readonly IAppStartup _appStartup = appStartup;
    private readonly ILogger<OutboxMonitor> _logger = logger;
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await _appStartup.WaitUntilStartupCompletedAsync(cancellationToken);

        _logger.LogInformation("{ServiceName} has started", nameof(OutboxMonitor));

        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _queue.Writer.TryWrite(0);
                await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
            }
        }, cancellationToken);

        await foreach (var value in _queue.Reader.ReadAllAsync(cancellationToken))
        {
            if (value == 0)
            {
                _logger.LogDebug("{ServiceName} triggered to check for messages to process periodically", nameof(OutboxMonitor));
            }
            else if (value == 1)
            {
                _logger.LogDebug("{ServiceName} triggered to check for messages to process immediately", nameof(OutboxMonitor));
            }
            else
            {
                _logger.LogWarning("{ServiceName} received unknown trigger value {Value}", nameof(OutboxMonitor), value);
            }

            try
            {
                while (await _dataSource.Dequeue(DEQUEUE_MSG_LIMIT, cancellationToken) is { Length: > 0 } messages)
                {
                    _logger.LogInformation("Received {Count} messages from the outbox to process", messages.Length);
                    await foreach (var task in Task.WhenEach(messages.Select(msg => _processor.Process(msg, cancellationToken))))
                    {
                        if (task.Result.Succeeded(out var message))
                        {
                            _ = await _dataSource.Delete(message.MessageId, cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages.");
            }
        }

        _logger.LogInformation("{ServiceName} has stopped", nameof(OutboxMonitor));
    }
}
