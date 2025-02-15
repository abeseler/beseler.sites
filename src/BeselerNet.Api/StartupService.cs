﻿using Beseler.ServiceDefaults;
using Microsoft.Extensions.Caching.Hybrid;

namespace BeselerNet.Api;

internal sealed class StartupService(HybridCache cache, StartupHealthCheck startupCheck, ILogger<StartupService> logger) : BackgroundService
{
    private readonly HybridCache _cache = cache;
    private readonly StartupHealthCheck _startupCheck = startupCheck;
    private readonly ILogger<StartupService> _logger = logger;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _cache.GetOrCreateAsync("Startup:Api", async entry =>
        {
            await Task.CompletedTask;
            return true;
        }, cancellationToken: stoppingToken);
        await _cache.RemoveAsync("Startup:Api", stoppingToken);

        _logger.LogInformation("The startup service has completed.");
        _startupCheck.StartupCompleted = true;
    }
}
