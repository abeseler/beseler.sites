using System.Text.Json.Serialization;

namespace BeselerNet.Api.Webhooks;

internal static class EmailEventsWebhook
{
    public static async Task<IResult> Handle(EmailEvent[] events, ILogger<EmailEvent> logger, CancellationToken stoppingToken)
    {        
        foreach (var @event in events)
        {
            logger.LogInformation("Email event: {EmailEvent}", @event);
        }

        await Task.CompletedTask;
        return TypedResults.NoContent();
    }
}

internal sealed record EmailEvent
{
    public required string Email { get; init; }
    public required long Timestamp { get; init; }
    public required string Event { get; init; }
    [JsonPropertyName("smtp-id")]
    public string? SmtpId { get; init; }
    public string? Useragent { get; init; }
    [JsonPropertyName("sg_event_id")]
    public required string SgEventId { get; init; }
    [JsonPropertyName("sg_message_id")]
    public string? SgMessageId { get; init; }
    public string? Reason { get; init; }
    public string? Status { get; init; }
    public string? Response { get; init; }
    public string[] Category { get; init; } = [];
    [JsonPropertyName("bounce_classification")]
    public string? BounceClassification { get; init; }
}
