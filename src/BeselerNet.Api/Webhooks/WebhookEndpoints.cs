using BeselerNet.Api.Communications;
using static System.Net.Mime.MediaTypeNames;
using static Microsoft.AspNetCore.Http.StatusCodes;

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
            .Accepts<SendGridEmailEvent[]>(Application.Json)
            .Produces(Status204NoContent)
            .Produces(Status401Unauthorized);
    }
}
