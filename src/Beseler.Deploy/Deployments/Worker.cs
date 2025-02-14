using k8s;
using k8s.Models;
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
                await ProcessDeployment(request, stoppingToken);
            }
        }
    }

    private async Task ProcessDeployment(WebhookRequest request, CancellationToken stoppingToken)
    {
        try
        {
            var config = KubernetesClientConfiguration.InClusterConfig();
            using var client = new Kubernetes(config);

            var k8sNamespace = "default";
            var name = request.Repository?.Name;
            if (name is null)
            {
                _logger.LogWarning("Repository name is null");
                return;
            }

            var deployment = await client.ReadNamespacedDeploymentAsync(name, k8sNamespace, cancellationToken: stoppingToken);
            if (deployment is null)
            {
                _logger.LogWarning("Deployment {Name} not found", name);
                return;
            }

            var patch = new V1Patch($$"""
                {
                    "spec": {
                        "template": {
                            "metadata": {
                                "annotations": {
                                    "kubectl.kubernetes.io/restartedAt": "{{DateTime.UtcNow:o}}"
                                }
                            }
                        }
                    }
                }
                """, V1Patch.PatchType.MergePatch);

            var patchResult = await client.PatchNamespacedDeploymentAsync(patch, name, k8sNamespace, cancellationToken: stoppingToken);

            _logger.LogInformation("Rollout restart initiated for deployment {Name} in namespace {Namespace}.", name, k8sNamespace);

            await MonitorDeploymentRollout(client, k8sNamespace, name, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deployment for {Name}.", request.Repository);
        }
    }

    private async Task MonitorDeploymentRollout(Kubernetes client, string k8sNamespace, string? name, CancellationToken stoppingToken)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            if (cts.Token.IsCancellationRequested)
            {
                _logger.LogWarning("Rollout restart for deployment {Name} in namespace {Namespace} timed out.", name, k8sNamespace);
                break;
            }

            var updatedDeployment = await client.ReadNamespacedDeploymentAsync(name, k8sNamespace, cancellationToken: cts.Token);
            var condition = updatedDeployment.Status?.Conditions?.FirstOrDefault(c => c.Type == "Progressing" || c.Type == "Available");
            if (condition is null)
            {
                _logger.LogWarning("Rollout restart for deployment {Name} in namespace {Namespace} failed.", name, k8sNamespace);
                break;
            }
            if (condition.Type == "Available" && condition.Status == "True")
            {
                _logger.LogInformation("Rollout restart for deployment {Name} in namespace {Namespace} completed successfully.", name, k8sNamespace);
                break;
            }
            if (condition.Type == "Progressing" && condition.Status == "False")
            {
                _logger.LogWarning("Rollout restart for deployment {Name} in namespace {Namespace} failed.", name, k8sNamespace);
                break;
            }

            _logger.LogInformation("Rollout restart for deployment {Name} in namespace {Namespace} is in progress. Status: {Status}, Reason: {Reason}.", name, k8sNamespace, condition.Status, condition.Reason);
        }
    }
}
