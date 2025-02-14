using System.Threading.Channels;

namespace Beseler.Deploy.Deployments;

internal sealed class Webhook
{
    public static async Task<IResult> Handle(WebhookRequest request, Channel<WebhookRequest> channel, ILogger<Webhook> logger)
    {
        logger.LogInformation("Received webhook for {Repository} at {PushedAt}", request.Repository.RepoName, request.PushData.PushedAt);

        await channel.Writer.WriteAsync(request);

        return Results.NoContent();
    }
}

internal static class WebhookExtensions
{
    public static void MapDeployWebhook(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhook", Webhook.Handle)
            .WithName("DeployWebhook")
            .WithTags("Deployment");
    }
}
