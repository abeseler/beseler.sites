using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Beseler.ServiceDefaults;
public sealed class StartupHealthCheck : IHealthCheck
{
    private volatile bool _isReady;
    public bool StartupCompleted
    {
        get => _isReady;
        set => _isReady = value;
    }
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(StartupCompleted
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The startup task is still running."));
}
