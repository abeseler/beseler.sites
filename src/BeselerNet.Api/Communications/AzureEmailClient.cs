using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;

namespace BeselerNet.Api.Communications;

internal sealed record AzureOptions
{
    public const string SectionName = "Azure";
    public string? ConnectionString { get; init; }
    public string? WebhookApiKey { get; init; }
}

internal sealed class AzureEmailClient(
    IOptions<AzureOptions> options,
    IOptions<CommunicationOptions> commOptions,
    CommunicationDataSource communications,
    ILogger<AzureEmailClient> logger) : IEmailClient
{
    public const string PROVIDER_NAME = "Azure";
    private readonly AzureOptions _options = options.Value;
    private readonly CommunicationOptions _commOptions = commOptions.Value;
    private readonly CommunicationDataSource _communications = communications;
    private readonly ILogger<AzureEmailClient> _logger = logger;

    public async Task<Communication> Send(EmailTemplate template, int accountId, string email, string recipientName, CancellationToken cancellationToken)
    {
        var communication = Communication.Create(PROVIDER_NAME, CommunicationType.Email, template.CommunicationName, accountId);

        try
        {
            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                _logger.LogWarning("{CommunicationName} not sent because Azure ConnectionString is missing.", template.CommunicationName);
                communication.Failed(DateTimeOffset.UtcNow, "Azure ConnectionString is missing");
                await _communications.SaveChanges(communication, cancellationToken);
                return communication;
            }

            var client = new EmailClient(_options.ConnectionString);
            var content = new EmailContent(template.Subject)
            {
                PlainText = template.PlainTextContent,
                Html = template.HtmlContent
            };
            var emailMessage = new EmailMessage(_commOptions.SenderEmail, new EmailRecipients([new EmailAddress(email, recipientName)]), content);

            var operation = await client.SendAsync(WaitUntil.Started, emailMessage, cancellationToken);
            communication.Sent(DateTimeOffset.UtcNow, operation.Id);

            _logger.LogInformation("Email {CommunicationName} sent to {Email}", template.CommunicationName, email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {CommunicationName} email to {Email}", template.CommunicationName, email);
            communication.Failed(DateTimeOffset.UtcNow, ex.Message);
        }
        finally
        {
            await _communications.SaveChanges(communication, cancellationToken);
        }

        return communication;
    }
}
