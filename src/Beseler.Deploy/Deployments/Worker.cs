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
                _logger.LogWarning("Deployment {Name} in namespace {Namespace} timed out.", name, k8sNamespace);
                break;
            }

            var updatedDeployment = await client.ReadNamespacedDeploymentAsync(name, k8sNamespace, cancellationToken: cts.Token);
            var condition = updatedDeployment.Status?.Conditions?.FirstOrDefault(c => c.Type == "Progressing" || c.Type == "Available");
            if (condition is null)
            {
                _logger.LogWarning("Deployment {Name} in namespace {Namespace} failed.", name, k8sNamespace);
                break;
            }
            if (condition.Type == "Available" && condition.Status == "True")
            {
                _logger.LogInformation("Deployment {Name} in namespace {Namespace} updated successfully.", name, k8sNamespace);
                break;
            }
            if (condition.Type == "Progressing" && condition.Status == "False")
            {
                _logger.LogWarning("Deployment {Name} in namespace {Namespace} failed.", name, k8sNamespace);
                break;
            }

            _logger.LogInformation("Deployment {Name} in namespace {Namespace} is in progress. Status: {Status}, Reason: {Reason}.", name, k8sNamespace, condition.Status, condition.Reason);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var deployment = await client.ReadNamespacedDeploymentAsync(name, k8sNamespace, cancellationToken: stoppingToken);
            var desired = deployment.Status?.UpdatedReplicas;

            var pods = await client.ListNamespacedPodAsync(k8sNamespace, labelSelector: $"app={name}", cancellationToken: stoppingToken);
            var total = pods.Items.Count;
            var running = pods.Items.Count(p => p.Status?.Phase == "Running");
            var ready = pods.Items.Count(p => p.Status?.Phase == "Running" && p.Status?.ContainerStatuses?.All(c => c.Ready) == true);
            var terminating = pods.Items.Count(p => p.Status?.Phase == "Terminating");
            var crashlooping = pods.Items.Count(p => p.Status?.Phase == "CrashLoopBackOff" || p.Status?.Phase == "Running" && p.Status?.ContainerStatuses?.Any(c => c.State?.Waiting?.Reason == "CrashLoopBackOff") == true);

            if (desired == total && ready == total && terminating == 0 && crashlooping == 0)
            {
                _logger.LogInformation("Deployment {Name} in namespace {Namespace} updated successfully. Desired: {Desired}, Total: {Total}, Running: {Running}, Ready: {Ready}.", name, k8sNamespace, desired, total, running, ready);
                break;
            }
            if (crashlooping > 0)
            {
                _logger.LogError("Deployment {Name} in namespace {Namespace} failed. Crashlooping pods: {Crashlooping}.", name, k8sNamespace, crashlooping);
                break;
            }

            _logger.LogInformation("Deployment {Name} in namespace {Namespace} is in progress. Desired: {Desired}, Total: {Total}, Running: {Running}, Ready: {Ready}, Terminating: {Terminating}", name, k8sNamespace, desired, total, running, ready, terminating);

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
