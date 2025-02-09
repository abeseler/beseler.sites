using Beseler.ServiceDefaults;
using Microsoft.Extensions.Caching.Hybrid;

namespace BeselerNet.Api;

internal sealed class StartupService(HybridCache cache, StartupHealthCheck startupCheck, ILogger<StartupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await cache.GetOrCreateAsync("Startup:Api", async entry =>
        {
            await Task.CompletedTask;
            return true;
        }, cancellationToken: stoppingToken);
        await cache.RemoveAsync("Startup:Api", stoppingToken);

        logger.LogInformation("The startup service has completed.");
        startupCheck.StartupCompleted = true;
    }
}
