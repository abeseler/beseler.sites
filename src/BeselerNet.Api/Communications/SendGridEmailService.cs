using BeselerNet.Shared.Core;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BeselerNet.Api.Communications;

internal sealed record SendGridOptions
{
    public const string SectionName = "SendGrid";
    public string? ApiKey { get; init; }
    public string? WebhookApiKey { get; init; }
}

internal sealed class SendGridEmailService(
    CommunicationDataSource communications,
    IOptions<CommunicationOptions> commOptions,
    ISendGridClient client,
    IOptions<SendGridOptions> options,
    ILogger<SendGridEmailService> logger) : IEmailer
{
    private const string PROVIDER_NAME = "SendGrid";
    private readonly CommunicationDataSource _communications = communications;
    private readonly CommunicationOptions _commOptions = commOptions.Value;
    private readonly ISendGridClient _client = client;
    private readonly SendGridOptions _options = options.Value;
    private readonly ILogger<SendGridEmailService> _logger = logger;

    public async Task<Result<Communication>> SendEmailVerification(int accountId, string email, string recipientName, string token, CancellationToken cancellationToken)
    {
        var template = EmailTemplates.EmailVerification(_commOptions.ConfirmEmailUrl!, token);
        var communication = await Send(template, accountId, email, recipientName, cancellationToken);
        if (communication.FailedAt.HasValue)
        {
            _logger.LogInformation("Token not sent: {Token}", token);
        }
        return communication;
    }
    public async Task<Result<Communication>> SendAccountLocked(int accountId, string email, string recipientName, CancellationToken cancellationToken)
    {
        var template = EmailTemplates.AccountLocked(recipientName);
        return await Send(template, accountId, email, recipientName, cancellationToken);
    }
    public async Task<Result<Communication>> SendPasswordReset(int accountId, string email, string recipientName, string token, CancellationToken cancellationToken)
    {
        var template = EmailTemplates.PasswordReset(recipientName, _commOptions.ResetPasswordUrl!, token);
        var communication = await Send(template, accountId, email, recipientName, cancellationToken);
        if (communication.FailedAt.HasValue)
        {
            _logger.LogInformation("Token not sent: {Token}", token);
        }
        return communication;
    }
    private async Task<Communication> Send(EmailTemplate template, int accountId, string email, string recipientName, CancellationToken cancellationToken)
    {
        var communication = Communication.Create(PROVIDER_NAME, CommunicationType.Email, template.CommunicationName, accountId);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("{CommunicationName} not sent because SendGrid ApiKey is missing.", template.CommunicationName);
            communication.Failed(DateTimeOffset.UtcNow, "SendGrid ApiKey is missing");
            await _communications.SaveChanges(communication, cancellationToken);
            return communication;
        }

        try
        {
            var emailMessage = new SendGridMessage
            {
                From = new EmailAddress(_commOptions.SenderEmail, _commOptions.SenderName),
                Subject = template.Subject,
                PlainTextContent = template.PlainTextContent,
                HtmlContent = template.HtmlContent,
                CustomArgs = new()
                {
                    ["communication_id"] = communication.CommunicationId.ToString()
                }
            };
            emailMessage.AddTo(new EmailAddress(email, recipientName));

            var response = await _client.SendEmailAsync(emailMessage, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Sent {CommunicationName} email to {Email}", template.CommunicationName, email);
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);
                communication.Failed(DateTimeOffset.UtcNow, $"Failed to send: {responseBody}");
                _logger.LogError("Failed to send {CommunicationName} email: {Response}", template.CommunicationName, responseBody);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {CommunicationName} email to {Email}", template.CommunicationName, email);
            communication.Failed(DateTimeOffset.UtcNow, ex.Message);
        }

        await _communications.SaveChanges(communication, cancellationToken);
        return communication;
    }
}
