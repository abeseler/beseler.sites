namespace Beseler.Deploy.Endpoints;

internal sealed class DeployWebhook
{
    public static async Task<IResult> Handle(DeployWebhookRequest request, ILogger<DeployWebhook> logger)
    {
        logger.LogInformation("Received webhook for {Repository} at {PushedAt}", request.Repository.RepoName, request.PushData.PushedAt);
        logger.LogInformation("Request: {Request}", request);

        await Task.Delay(100);
        return Results.NoContent();
    }
}

internal static class DeployWebhookExtensions
{
    public static void MapDeployWebhook(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhook", DeployWebhook.Handle)
            .WithName("DeployWebhook")
            .WithTags("Deployment");
    }
}

public sealed record class DeployWebhookRequest(string CallbackUrl, PushData PushData, Repository Repository);
public sealed record class PushData(long PushedAt, string Pusher, string Tag);
public sealed record class Repository(
    int CommentCount,
    long DateCreated,
    string Description,
    string Dockerfile,
    string FullDescription,
    bool IsOfficial,
    bool IsPrivate,
    bool IsTrusted,
    string Name,
    string Namespace,
    string Owner,
    string RepoName,
    string RepoUrl,
    int StarCount,
    string Status);
