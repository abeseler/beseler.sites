using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;

namespace BeselerNet.Api.Communications.Emails;

internal sealed record AzureOptions
{
    public const string SectionName = "Azure";
    public string? CommunicationConnectionString { get; init; }
    public string? WebhookApiKey { get; init; }
}

internal sealed class AzureEmailClient(IOptions<AzureOptions> options, ILogger<AzureEmailClient> logger) : IEmailClient
{
    public string ProviderName { get; } = "Azure";
    private readonly AzureOptions _options = options.Value;
    private readonly ILogger<AzureEmailClient> _logger = logger;

    public async Task Send(Communication communication, EmailTemplate template, string email, string recipientName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.CommunicationConnectionString))
        {
            throw new InvalidOperationException("Azure CommunicationConnectionString is not configured.");
        }

        var client = new EmailClient(_options.CommunicationConnectionString);
        var content = new EmailContent(template.Subject)
        {
            PlainText = template.PlainTextContent,
            Html = template.HtmlContent
        };
        var emailMessage = new EmailMessage(template.SenderEmail, new EmailRecipients([new EmailAddress(email, recipientName)]), content);

        var operation = await client.SendAsync(WaitUntil.Started, emailMessage, ct);
        communication.Sent(DateTimeOffset.UtcNow, operation.Id);

        _logger.LogInformation("Email {CommunicationName} sent to {Email}", template.CommunicationName, email);
    }
}
