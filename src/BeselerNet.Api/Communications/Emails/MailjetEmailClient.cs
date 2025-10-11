using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace BeselerNet.Api.Communications.Emails;

internal sealed record MailjetOptions
{
    public const string SectionName = "Mailjet";
    public string? ApiKey { get; init; }
    public string? ApiSecret { get; init; }
    public string? WebhookApiKey { get; init; }
}

internal sealed class MailjetEmailClient(IMailjetClient client, IOptions<MailjetOptions> options, ILogger<MailjetEmailClient> logger) : IEmailClient
{
    public string ProviderName { get; } = "Mailjet";
    private readonly IMailjetClient _client = client;
    private readonly MailjetOptions _options = options.Value;
    private readonly ILogger<MailjetEmailClient> _logger = logger;

    public async Task Send(Communication communication, EmailTemplate template, string email, string recipientName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            communication.Failed(DateTimeOffset.UtcNow, "Mailjet ApiKey is missing");
            _logger.LogWarning("{CommunicationName} not sent because Mailjet ApiKey is missing.", template.CommunicationName);
            return;
        }

        var request = new MailjetRequest{ Resource = SendV31.Resource };        
        request.Property(Mailjet.Client.Resources.Send.Messages,
            new JArray {
                new JObject {
                    {"From", new JObject {
                        {"Email", template.SenderEmail},
                        {"Name", template.SenderName}
                    }},
                    {"To", new JArray {
                        new JObject {
                            {"Email", email},
                            {"Name", recipientName}
                        }
                    }},
                    {"Subject", template.Subject},
                    {"TextPart", template.PlainTextContent},
                    {"HTMLPart", template.HtmlContent},
                    {"CustomID", communication.CommunicationId.ToString()}
                }
            });

        var response = await _client.PostAsync(request);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Email {CommunicationName} sent to {Email}", template.CommunicationName, email);
        }
        else
        {
            var errorMessage = response.GetErrorMessage();
            communication.Failed(DateTimeOffset.UtcNow, errorMessage);
            _logger.LogError("Failed to send {CommunicationName} email to {Email}: {StatusCode} - {ResponseData}", template.CommunicationName, email, response.StatusCode, response.GetData());
        }
    }
}
