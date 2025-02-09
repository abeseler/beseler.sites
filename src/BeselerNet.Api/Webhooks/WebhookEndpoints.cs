namespace BeselerNet.Api.Webhooks;

internal static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/webhooks");

        group.MapPost("/email-events", () => { return TypedResults.NoContent(); })
            .WithName("PostEmailEvent")
            .WithTags("Webhooks");
    }
}
