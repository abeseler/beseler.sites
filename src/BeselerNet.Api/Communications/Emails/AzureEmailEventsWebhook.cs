using Azure.Messaging;
using Azure.Messaging.EventGrid.SystemEvents;
using BeselerNet.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BeselerNet.Api.Communications.Emails;

internal sealed class AzureEmailEventsWebhook
{
    private static readonly GenericMessageResponse s_okResponse = new() { Message = "Event processed." };

    public static async Task<IResult> ValidationHandshake(
        [FromHeader(Name = "WebHook-Request-Origin")] string? origin,
        [FromHeader(Name = "WebHook-Request-Callback")] string? callback,
        [FromHeader(Name = "WebHook-Request-Rate")] string? rate,
        HttpResponse response,
        CancellationToken cancellationToken
    )
    {
        if (!string.IsNullOrWhiteSpace(callback))
        {
            using var httpClient = new HttpClient();
            using var httpResponse = await httpClient.PostAsync(callback, null, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                return TypedResults.Problem($"Callback to {callback} failed with status code {httpResponse.StatusCode}");
            }
        }

        var allowedRate = rate is not null && int.TryParse(rate, out var r) && r <= 100 ? r : 100;

        response.Headers.Allow = "POST, OPTIONS";
        response.Headers.Append("WebHook-Allowed-Origin", origin ?? "*");
        response.Headers.Append("WebHook-Allowed-Rate", allowedRate.ToString());

        return TypedResults.Ok();
    }

    public static async Task<IResult> Handle(
        CloudEvent cloudEvent,
        string? apikey,
        CommunicationDataSource dataSource,
        IOptions<AzureOptions> options,
        ILogger<AzureEmailEventsWebhook> logger,
        CancellationToken ct)
    {
        var validApiKey = options.Value.WebhookApiKey;
        if (validApiKey is { Length: > 0 } && (apikey is null || apikey != validApiKey))
        {
            return TypedResults.Unauthorized();
        }

        var task = cloudEvent.Type switch
        {
            "Microsoft.Communication.EmailDeliveryReportReceived" => HandleEmailDeliveryReportReceived(cloudEvent, dataSource, logger, ct),
            "Microsoft.EventGrid.EmailEngagementTrackingReportReceived" => HandleEmailEngagementTrackingReportReceived(cloudEvent, dataSource, logger, ct),
            _ => Task.CompletedTask
        };
        await task;

        return TypedResults.Ok(s_okResponse);
    }

    private static async Task HandleEmailDeliveryReportReceived(CloudEvent cloudEvent, CommunicationDataSource dataSource, ILogger logger, CancellationToken ct)
    {
        var eventData = cloudEvent.Data?.ToObjectFromJson<AcsEmailDeliveryReportReceivedEventData>();
        if (eventData is null)
        {
            logger.LogWarning("Email delivery report received with no data.");
            return;
        }

        var communication = await dataSource.WithExternalId(eventData.MessageId, ct);
        if (communication is null)
        {
            logger.LogWarning("Email delivery report received for unknown message ID {MessageId}.", eventData.MessageId);
            return;
        }

        if (eventData.Status == AcsEmailDeliveryReportStatus.Delivered)
        {
            communication.Delivered(eventData.DeliveryAttemptTimestamp ?? DateTimeOffset.UtcNow);
        }
        else
        {
            communication.Failed(eventData.DeliveryAttemptTimestamp ?? DateTimeOffset.UtcNow, $"{eventData.Status}. {eventData.DeliveryStatusDetails.StatusMessage}".TrimEnd());
        }

        await dataSource.SaveChanges(communication, ct);
    }

    private static async Task HandleEmailEngagementTrackingReportReceived(CloudEvent cloudEvent, CommunicationDataSource dataSource, ILogger logger, CancellationToken ct)
    {
        var eventData = cloudEvent.Data?.ToObjectFromJson<AcsEmailEngagementTrackingReportReceivedEventData>();
        if (eventData is null)
        {
            logger.LogWarning("Email engagement tracking report received with no data.");
            return;
        }

        var communication = await dataSource.WithExternalId(eventData.MessageId, ct);
        if (communication is null)
        {
            logger.LogWarning("Email engagement tracking report received for unknown message ID {MessageId}.", eventData.MessageId);
            return;
        }

        communication.Opened(eventData.UserActionTimestamp ?? DateTimeOffset.UtcNow);
    }
}
