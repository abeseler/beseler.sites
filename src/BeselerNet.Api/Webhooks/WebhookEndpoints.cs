using BeselerNet.Api.Webhooks.Handlers;
using System.Net.Mime;

namespace BeselerNet.Api.Webhooks;

internal static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder builder)
    {
        var v1 = builder.MapGroup("/v1/webhooks")
            .WithTags("Webhooks");

        v1.MapPost("/email-events", EmailEventsWebhook.Handle)
            .WithName("ProcessEmailEvents")
            .Accepts<EmailEvent[]>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status204NoContent);
    }
}
