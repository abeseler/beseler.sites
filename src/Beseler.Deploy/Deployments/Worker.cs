using System.Threading.Channels;

namespace Beseler.Deploy.Deployments;

public class Worker(Channel<WebhookRequest> channel, ILogger<Worker> logger) : BackgroundService
{
    private readonly Channel<WebhookRequest> _channel = channel;
    private readonly ILogger<Worker> _logger = logger;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _channel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (_channel.Reader.TryRead(out var request))
            {
                await ProcessDeployment(request);
            }
        }
    }

    private async Task ProcessDeployment(WebhookRequest request)
    {
        await Task.CompletedTask;
        _logger.LogInformation("Processing deployment for {Repository}:\n{Request}", request.Repository?.RepoName, request);
    }
}
