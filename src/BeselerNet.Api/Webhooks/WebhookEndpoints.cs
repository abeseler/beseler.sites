using BeselerNet.Api.Communications;
using BeselerNet.Shared.Contracts;
using System.Diagnostics;
using static Microsoft.AspNetCore.Http.StatusCodes;
using static System.Net.Mime.MediaTypeNames;

namespace BeselerNet.Api.Webhooks;

internal static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder builder)
    {
        var v1 = builder.MapGroup("/v1/webhooks")
            .WithTags("Webhooks");

        v1.MapPost("/mailjet-events", MailjetEmailEventsWebhook.Handle)
            .WithName("ProcessMailjetEmailEvents")
            .WithDescription("Process Mailjet email events.")
            .Accepts<MailjetEmailEvent[]>(Application.Json)
            .Produces<GenericMessageResponse>(Status200OK)
            .Produces(Status401Unauthorized);

        v1.MapPost("/azure-events", AzureEmailEventsWebhook.Handle)
            .WithName("ProcessAzureEmailEvents")
            .WithDescription("Process Azure email events.")
            .Produces<GenericMessageResponse>(Status200OK)
            .Produces(Status401Unauthorized);

        v1.MapMethods("/azure-events", ["OPTIONS"], ValidationHandshake)
            .WithName("ValidateAzureEventsWebhook")
            .WithDescription("Validation handshake for Azure email events webhook.")
            .Produces(Status200OK);
    }

    private static IResult ValidationHandshake() => Results.Ok();
}
