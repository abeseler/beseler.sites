using Beseler.ServiceDefaults;

namespace BeselerNet.Api.Core;

internal sealed class StartupService(StartupHealthCheck startupCheck, ILogger<StartupService> logger) : BackgroundService
{
    private readonly StartupHealthCheck _startupCheck = startupCheck;
    private readonly ILogger<StartupService> _logger = logger;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.CompletedTask;

        _logger.LogInformation("The startup service has completed.");
        _startupCheck.StartupCompleted = true;
    }
}
