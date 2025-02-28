using BeselerNet.Api.Core;
using BeselerNet.Shared.Contracts;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace BeselerNet.Api.Communications;

internal static class SendGridEmailEventsWebhook
{
    public static IResult Handle(string? apikey, SendGridEmailEvent[] events, IOptions<SendGridOptions> options)
    {
        var validApiKey = options.Value.WebhookApiKey;
        if (validApiKey is { Length: > 0 } && (apikey is null || apikey != validApiKey))
        {
            return TypedResults.Unauthorized();
        }

        var requestSubmitted = SendGridEmailEventService.RequestChannel.Writer.TryWrite(new SendGridEmailEventRequest(events));
        return requestSubmitted
            ? TypedResults.Ok(new GenericMessageResponse { Message = "Events submitted for processing." })
            : TypedResults.Problem(Problems.TooManyRequests);
    }
}

internal sealed class SendGridEmailEventService(IServiceProvider services, ILogger<SendGridEmailEventService> logger) : BackgroundService
{
    private readonly IServiceProvider _services = services;
    private readonly ILogger<SendGridEmailEventService> _logger = logger;

    public static readonly Channel<SendGridEmailEventRequest> RequestChannel = Channel.CreateBounded<SendGridEmailEventRequest>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SendGrid email event service started");

        while (await RequestChannel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (RequestChannel.Reader.TryRead(out var request))
            {
                await ProcessRequest(request, stoppingToken);
            }
        }

        _logger.LogInformation("SendGrid email event service stopped");
    }

    private async Task ProcessRequest(SendGridEmailEventRequest request, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _services.CreateAsyncScope();
            var communications = scope.ServiceProvider.GetRequiredService<CommunicationDataSource>();

            foreach (var @event in request.Events)
            {
                if (!Guid.TryParse(@event.CommunicationId, out var communicationId))
                {
                    _logger.LogWarning("Event with invalid or no communication ID: {Data}", @event);
                    continue;
                }

                var communication = await communications.WithId(communicationId, stoppingToken);
                if (communication is null)
                {
                    _logger.LogWarning("Communication record missing for ID: {CommunicationId}", communicationId);
                    continue;
                }

                var eventDate = DateTimeOffset.FromUnixTimeSeconds(@event.Timestamp);
                switch (@event.Event)
                {
                    case "processed":
                        communication.Sent(eventDate);
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
                        _logger.LogWarning("Unhandled event type: {Event}", @event.Event);
                        break;
                }

                if (@event.SgMessageId is not null)
                {
                    communication.SetExternalId(@event.SgMessageId);
                }

                await communications.SaveChanges(communication, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SendGrid email event request: {Message}", ex.Message);
        }
    }
}

internal readonly struct SendGridEmailEventRequest(SendGridEmailEvent[] events)
{
    public SendGridEmailEvent[] Events { get; } = events;
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
