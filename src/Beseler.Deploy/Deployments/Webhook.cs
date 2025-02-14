using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace Beseler.Deploy.Deployments;

internal sealed class Webhook
{
    public static async Task<IResult> Handle(string key, WebhookRequest request, Channel<WebhookRequest> channel, IOptions<WebhookOptions> options, ILogger<Webhook> logger)
    {
        var secret = options.Value.Secret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            logger.LogWarning("Missing webhook secret, skipping authentication");
        }
        else if (!secret.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Invalid webhook key ({key}), skipping request", key);
            return Results.Unauthorized();
        }

        logger.LogInformation("Received webhook request for {RepositoryName}", request.Repository?.RepoName);
        await channel.Writer.WriteAsync(request);
        return Results.NoContent();
    }
}

internal static class WebhookExtensions
{
    public static void MapDeployWebhook(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhook/{key}", Webhook.Handle)
            .WithName("DeployWebhook")
            .WithTags("Deployment");
    }
}

public sealed record WebhookOptions
{
    public string Secret { get; init; } = "";
}
