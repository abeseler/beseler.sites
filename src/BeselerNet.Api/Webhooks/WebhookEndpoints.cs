using BeselerNet.Api.Communications;
using BeselerNet.Shared.Contracts;
using static Microsoft.AspNetCore.Http.StatusCodes;
using static System.Net.Mime.MediaTypeNames;

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
            .Produces<GenericMessageResponse>(Status200OK)
            .Produces(Status401Unauthorized);

        _ = v1.MapPost("/mailjet-events", MailjetEmailEventsWebhook.Handle)
            .WithName("ProcessMailjetEmailEvents")
            .WithDescription("Process Mailjet email events.")
            .Accepts<MailjetEmailEvent[]>(Application.Json)
            .Produces<GenericMessageResponse>(Status200OK)
            .Produces(Status401Unauthorized);
    }
}
