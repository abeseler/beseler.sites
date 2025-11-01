using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beseler.ServiceDefaults;

public interface IStartupTask
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

public interface IAppStartup
{
    bool IsCompleted { get; }
    void StartupCompleted();
    Task WaitUntilStartupCompletedAsync(CancellationToken cancellationToken);
}

internal sealed class ServiceStartup : IAppStartup
{
    private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool IsCompleted => _tcs.Task.IsCompleted;    
    public void StartupCompleted() => _tcs.TrySetResult();
    public Task WaitUntilStartupCompletedAsync(CancellationToken cancellationToken) => _tcs.Task;

    public sealed class HealthCheck(IAppStartup startup) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(startup.IsCompleted
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("The startup task is still running."));
    }
}

internal sealed class DefaultStartupService(IAppStartup appStartup, IServiceScopeFactory scopeFactory, ILogger<DefaultStartupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var startupTasks = scope.ServiceProvider.GetServices<IStartupTask>().ToArray();

        if (startupTasks.Length == 0)
        {
            logger.LogInformation("No startup tasks to execute.");
            appStartup.StartupCompleted();
            return;
        }

        logger.LogInformation("Executing {Count} startup tasks...", startupTasks.Length);

        await Task.WhenAll(startupTasks.Select(t => t.ExecuteAsync(cancellationToken)));

        logger.LogInformation("All startup tasks have completed.");

        appStartup.StartupCompleted();
    }
}
