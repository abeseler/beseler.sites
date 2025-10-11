using Beseler.ServiceDefaults;
using BeselerNet.Api.Core;
using BeselerNet.Shared.Contracts;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace BeselerNet.Api.Communications.Emails;

internal static class MailjetEmailEventsWebhook
{
    public static IResult Handle(string? apikey, MailjetEmailEvent[] events, IOptions<MailjetOptions> options)
    {
        var validApiKey = options.Value.WebhookApiKey;
        if (validApiKey is { Length: > 0 } && (apikey is null || apikey != validApiKey))
        {
            return TypedResults.Unauthorized();
        }

        var requestSubmitted = MailjetEmailEventService.RequestChannel.Writer.TryWrite(new MailjetEmailEventRequest(events));
        return requestSubmitted
            ? TypedResults.Ok(new GenericMessageResponse { Message = "Events submitted for processing." })
            : TypedResults.Problem(Problems.TooManyRequests);
    }
}

internal sealed class MailjetEmailEventService(IServiceProvider services, IAppStartup appStartup, ILogger<MailjetEmailEventService> logger) : BackgroundService
{
    private readonly IServiceProvider _services = services;
    private readonly IAppStartup _appStartup = appStartup;
    private readonly ILogger<MailjetEmailEventService> _logger = logger;

    public static readonly Channel<MailjetEmailEventRequest> RequestChannel = Channel.CreateBounded<MailjetEmailEventRequest>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await _appStartup.WaitUntilStartupCompletedAsync(cancellationToken);

        _logger.LogInformation("{ServiceName} started", nameof(MailjetEmailEventService));

        while (await RequestChannel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (RequestChannel.Reader.TryRead(out var request))
            {
                await ProcessRequest(request, cancellationToken);
            }
        }

        _logger.LogInformation("{ServiceName} stopped", nameof(MailjetEmailEventService));
    }

    private async Task ProcessRequest(MailjetEmailEventRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _services.CreateAsyncScope();
            var communications = scope.ServiceProvider.GetRequiredService<CommunicationDataSource>();

            foreach (var @event in request.Events)
            {
                if (!Guid.TryParse(@event.CustomID, out var communicationId))
                {
                    _logger.LogWarning("Event with invalid or no communication ID: {Data}", @event);
                    continue;
                }

                var communication = await communications.WithId(communicationId, cancellationToken);
                if (communication is null)
                {
                    _logger.LogWarning("Communication record missing for ID: {CommunicationId}", communicationId);
                    continue;
                }

                var eventDate = DateTimeOffset.FromUnixTimeSeconds(@event.Time);
                switch (@event.Event)
                {
                    case "sent":
                        communication.Sent(eventDate);
                        break;
                    case "open":
                        communication.Opened(eventDate);
                        break;
                    case "blocked":
                        communication.Failed(eventDate, $"(Blocked) {@event.Error} {@event.Comment}");
                        break;
                    case "bounce":
                        communication.Failed(eventDate, $"(Bounced) {@event.Error} {@event.Comment}");
                        break;
                    default:
                        _logger.LogWarning("Unhandled event type: {Event}", @event.Event);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Mailjet email event request: {Message}", ex.Message);
        }
    }
}

internal readonly struct MailjetEmailEventRequest(MailjetEmailEvent[] events)
{
    public MailjetEmailEvent[] Events { get; } = events;
}

/// <summary>
/// Model for Mailjet email events.
/// <para>Documentation: <see href="https://dev.mailjet.com/email/guides/webhooks/"/></para>
/// </summary>
internal sealed record MailjetEmailEvent
{
    public string Event { get; init; } = "unknown";
    public long? MessageID { get; init; }
    public long Time { get; init; }
    public string? Email { get; init; }
    public string? CustomID { get; init; }
    public string? Error { get; init; }
    public string? Comment { get; init; }
}
