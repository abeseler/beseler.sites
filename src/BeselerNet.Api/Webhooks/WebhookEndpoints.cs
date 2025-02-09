using System.Net.Mime;

namespace BeselerNet.Api.Webhooks;

internal static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/webhooks")
            .WithTags("Webhooks");

        group.MapPost("/email-events", EmailEventsWebhook.Handle)
            .WithName("PostEmailEvents")
            .Accepts<EmailEvent[]>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status204NoContent);
    }
}
