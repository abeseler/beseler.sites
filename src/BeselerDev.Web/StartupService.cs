﻿using Beseler.ServiceDefaults;

namespace BeselerDev.Web;

internal sealed class StartupService(StartupHealthCheck startupCheck, ILogger<StartupService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("The startup service has completed.");
        startupCheck.StartupCompleted = true;
        return Task.CompletedTask;
    }
}
