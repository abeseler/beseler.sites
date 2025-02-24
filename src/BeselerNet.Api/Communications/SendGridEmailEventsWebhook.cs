using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace BeselerNet.Api.Communications;

internal static class SendGridEmailEventsWebhook
{
    public static async Task<IResult> Handle(string? apikey, SendGridEmailEvent[] events, CommunicationDataSource communications, IOptions<SendGridOptions> options, ILogger<SendGridEmailEvent> logger, CancellationToken stoppingToken)
    {
        var validApiKey = options.Value.WebhookApiKey;
        if (validApiKey is { Length: > 0 } && (apikey is null || apikey != validApiKey))
        {
            return TypedResults.Unauthorized();
        }

        foreach (var @event in events)
        {
            if (!Guid.TryParse(@event.CommunicationId, out var communicationId))
            {
                logger.LogWarning("Event with invalid or no communication ID: {Data}", @event);
                continue;
            }

            var communication = await communications.WithId(communicationId, stoppingToken);
            if (communication is null)
            {
                logger.LogWarning("Communication record missing for ID: {CommunicationId}", communicationId);
                continue;
            }

            var eventDate = DateTimeOffset.FromUnixTimeSeconds(@event.Timestamp);
            switch (@event.Event)
            {
                case "processed":
                    communication.Processed(eventDate);
                    break;
                case "delivered":
                    communication.Delivered(eventDate);
                    break;
                case "open":
                    communication.Opened(eventDate);
                    break;
                case "dropped":
                    communication.Failed(eventDate, $"(Dropped) {@event.Reason ?? "Missing reason"}");
                    break;
                case "deferred":
                    communication.Failed(eventDate, $"(Deferred) {@event.Attempt ?? @event.Status}");
                    break;
                case "bounce":
                    var type = @event.Type is "blocked" ? "Blocked" : "Bounced";
                    communication.Failed(eventDate, $"({type}) {@event.BounceClassification ?? "Missing classification"}");
                    break;
                default:
                    logger.LogWarning("Unhandled event type: {Event}", @event.Event);
                    break;
            }

            await communications.SaveChanges(communication, stoppingToken);
        }

        return TypedResults.NoContent();
    }
}

/// <summary>
/// Model for SendGrid email events.
/// <para>Documentation: <see href="https://www.twilio.com/docs/sendgrid/for-developers/tracking-events/event"/></para>
/// </summary>
internal sealed record SendGridEmailEvent
{
    public required string Email { get; init; }
    public required long Timestamp { get; init; }
    public required string Event { get; init; }
    public required string? Type { get; init; }
    [JsonPropertyName("smtp-id")]
    public string? SmtpId { get; init; }
    public string? Useragent { get; init; }
    [JsonPropertyName("sg_event_id")]
    public required string SgEventId { get; init; }
    [JsonPropertyName("sg_message_id")]
    public string? SgMessageId { get; init; }
    public string? Reason { get; init; }
    public string? Attempt { get; init; }
    public string? Status { get; init; }
    public string? Response { get; init; }
    public string[] Category { get; init; } = [];
    [JsonPropertyName("bounce_classification")]
    public string? BounceClassification { get; init; }
    [JsonPropertyName("communication_id")]
    public string? CommunicationId { get; init; }
}
