using Beseler.ServiceDefaults;
using k8s;
using k8s.Models;
using System.Threading.Channels;

namespace Beseler.Deploy.Deployments;

public class Worker(Channel<WebhookRequest> channel, IAppStartup appStartup, ILogger<Worker> logger) : BackgroundService
{
    private readonly Channel<WebhookRequest> _channel = channel;
    private readonly IAppStartup _appStartup = appStartup;
    private readonly ILogger<Worker> _logger = logger;
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await _appStartup.WaitUntilStartupCompletedAsync(cancellationToken);

        _logger.LogInformation("Deployment worker started.");

        while (await _channel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_channel.Reader.TryRead(out var request))
            {
                if (request.Repository?.Name == "beseler-net-dbdeploy")
                {
                    await ProcessJob(request, cancellationToken);
                }
                else
                {
                    await ProcessDeployment(request, cancellationToken);
                }
            }
        }
    }

    private async Task ProcessJob(WebhookRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var config = KubernetesClientConfiguration.InClusterConfig();
            using var client = new Kubernetes(config);

            var k8sNamespace = "default";

            var yaml = $"""
                apiVersion: batch/v1
                kind: Job
                metadata:
                  name: beseler-net-dbdeploy-{DateTime.UtcNow:yyMMddHHmmss}
                spec:
                  ttlSecondsAfterFinished: 300
                  template:
                    spec:
                      containers:
                      - name: beseler-net-dbdeploy
                        image: abeseler/beseler-net-dbdeploy:latest
                        imagePullPolicy: Always
                        envFrom:
                          - configMapRef:
                              name: beseler-net-dbdeploy-config
                      restartPolicy: Never
                  backoffLimit: 0 
                """;

            var job = KubernetesYaml.Deserialize<V1Job>(yaml);
            var result = await client.CreateNamespacedJobAsync(job, k8sNamespace, cancellationToken: cancellationToken);
            _logger.LogInformation("Job {Name} created in namespace {Namespace}.", result.Metadata.Name, k8sNamespace);

            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                if (cts.Token.IsCancellationRequested)
                {
                    _logger.LogWarning("Job {Name} timed out.", result.Metadata.Name);
                    break;
                }
                var updatedJob = await client.ReadNamespacedJobAsync(result.Metadata.Name, k8sNamespace, cancellationToken: cts.Token);
                if (updatedJob.Status?.Succeeded == 1)
                {
                    _logger.LogInformation("Job {Name} completed successfully.", result.Metadata.Name);
                    break;
                }
                if (updatedJob.Status?.Failed == 1)
                {
                    _logger.LogError("Job {Name} failed.", result.Metadata.Name);
                    break;
                }
                _logger.LogInformation("Job {Name} is in progress. Status: {Status}.", result.Metadata.Name, updatedJob.Status?.Conditions?.FirstOrDefault()?.Status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job for {Name}.", request.Repository);
        }
    }

    private async Task ProcessDeployment(WebhookRequest request, CancellationToken cancellationToken)
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

            var deployment = await client.ReadNamespacedDeploymentAsync(name, k8sNamespace, cancellationToken: cancellationToken);
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

            var patchResult = await client.PatchNamespacedDeploymentAsync(patch, name, k8sNamespace, cancellationToken: cancellationToken);

            _logger.LogInformation("Deployment {Name} update applied.", name);

            await MonitorDeploymentRollout(client, k8sNamespace, name, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deployment for {Name}.", request.Repository);
        }
    }

    private async Task MonitorDeploymentRollout(Kubernetes client, string k8sNamespace, string? name, CancellationToken cancellationToken)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            if (cts.Token.IsCancellationRequested)
            {
                _logger.LogWarning("Deployment {Name} timed out.", name);
                break;
            }

            var updatedDeployment = await client.ReadNamespacedDeploymentAsync(name, k8sNamespace, cancellationToken: cts.Token);
            var condition = updatedDeployment.Status?.Conditions?.FirstOrDefault(c => c.Type == "Progressing" || c.Type == "Available");
            if (condition is null)
            {
                _logger.LogWarning("Deployment {Name} failed.", name);
                break;
            }
            if (condition.Type == "Available" && condition.Status == "True")
            {
                _logger.LogInformation("Deployment {Name} update initiated.", name);
                break;
            }
            if (condition.Type == "Progressing" && condition.Status == "False")
            {
                _logger.LogWarning("Deployment {Name} failed.", name);
                break;
            }

            _logger.LogInformation("Deployment {Name} is in progress. Status: {Status}, Reason: {Reason}.", name, condition.Status, condition.Reason);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var deployment = await client.ReadNamespacedDeploymentAsync(name, k8sNamespace, cancellationToken: cancellationToken);
            var desired = deployment.Status?.UpdatedReplicas;

            var pods = await client.ListNamespacedPodAsync(k8sNamespace, labelSelector: $"app={name}", cancellationToken: cancellationToken);
            var total = pods.Items.Count;
            var running = pods.Items.Count(p => p.Status?.Phase == "Running");
            var ready = pods.Items.Count(p => p.Status?.Phase == "Running" && p.Status?.ContainerStatuses?.All(c => c.Ready) == true);
            var terminating = pods.Items.Count(p => p.Status?.Phase == "Terminating");
            var crashlooping = pods.Items.Count(p => p.Status?.Phase == "CrashLoopBackOff" || p.Status?.Phase == "Running" && p.Status?.ContainerStatuses?.Any(c => c.State?.Waiting?.Reason == "CrashLoopBackOff") == true);

            if (desired == total && ready == total && terminating == 0 && crashlooping == 0)
            {
                _logger.LogInformation("Deployment {Name} updated successfully. Desired: {Desired}, Total: {Total}, Running: {Running}, Ready: {Ready}.", name, desired, total, running, ready);
                break;
            }
            if (crashlooping > 0)
            {
                _logger.LogError("Deployment {Name} failed. Crashlooping pods: {Crashlooping}.", name, crashlooping);
                break;
            }

            _logger.LogInformation("Deployment {Name} is in progress. Desired: {Desired}, Total: {Total}, Running: {Running}, Ready: {Ready}, Terminating: {Terminating}", name, desired, total, running, ready, terminating);

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }
}
