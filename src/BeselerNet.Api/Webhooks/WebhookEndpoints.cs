using BeselerNet.Api.Communications;
using System.Net.Mime;

namespace BeselerNet.Api.Webhooks;

internal static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder builder)
    {
        var v1 = builder.MapGroup("/v1/webhooks")
            .WithTags("Webhooks");

        _ = v1.MapPost("/sendgrid-events", SendGridEmailEventsWebhook.Handle)
            .WithName("ProcessSendGridEmailEvents")
            .WithDescription("Process SendGrid email events.")
            .Accepts<SendGridEmailEvent[]>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
