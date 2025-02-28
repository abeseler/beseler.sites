using BeselerNet.Shared.Core;
using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Microsoft.Extensions.Options;

namespace BeselerNet.Api.Communications;

internal sealed record MailjetOptions
{
    public const string SectionName = "Mailjet";
    public string? ApiKey { get; init; }
    public string? ApiSecret { get; init; }
    public string? WebhookApiKey { get; init; }
}

internal sealed class MailjetEmailService(
    CommunicationDataSource communications,
    IOptions<CommunicationOptions> commOptions,
    IMailjetClient client,
    IOptions<MailjetOptions> options,
    ILogger<MailjetEmailService> logger) : IEmailer
{
    private const string PROVIDER_NAME = "Mailjet";
    private readonly CommunicationDataSource _communications = communications;
    private readonly CommunicationOptions _commOptions = commOptions.Value;
    private readonly IMailjetClient _client = client;
    private readonly MailjetOptions _options = options.Value;
    private readonly ILogger<MailjetEmailService> _logger = logger;

    public async Task<Result<Communication>> SendEmailVerification(int accountId, string email, string recipientName, string token, CancellationToken stoppingToken)
    {
        var template = EmailTemplates.EmailVerification(_commOptions.ConfirmEmailUrl!, token);
        var communication = await Send(template, accountId, email, recipientName, stoppingToken);
        if (communication.FailedAt.HasValue)
        {
            _logger.LogInformation("Token not sent: {Token}", token);
        }
        return communication;
    }
    public async Task<Result<Communication>> SendAccountLocked(int accountId, string email, string recipientName, CancellationToken stoppingToken)
    {
        var template = EmailTemplates.AccountLocked(recipientName);
        return await Send(template, accountId, email, recipientName, stoppingToken);
    }
    public async Task<Result<Communication>> SendPasswordReset(int accountId, string email, string recipientName, string token, CancellationToken stoppingToken)
    {
        var template = EmailTemplates.PasswordReset(recipientName, _commOptions.ResetPasswordUrl!, token);
        var communication = await Send(template, accountId, email, recipientName, stoppingToken);
        if (communication.FailedAt.HasValue)
        {
            _logger.LogInformation("Token not sent: {Token}", token);
        }
        return communication;
    }
    private async Task<Communication> Send(EmailTemplate template, int accountId, string email, string recipientName, CancellationToken stoppingToken)
    {
        var communication = Communication.Create(PROVIDER_NAME, CommunicationType.Email, template.CommunicationName, accountId);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("{CommunicationName} not sent because Mailjet ApiKey is missing.", template.CommunicationName);
            communication.Failed(DateTimeOffset.UtcNow, "Mailjet ApiKey is missing");
            await _communications.SaveChanges(communication, stoppingToken);
            return communication;
        }

        try
        {
            var emailMessage = new TransactionalEmailBuilder()
                .WithFrom(new SendContact(_commOptions.SenderEmail, _commOptions.SenderName))
                .WithSubject(template.Subject)
                .WithTextPart(template.PlainTextContent)
                .WithHtmlPart(template.HtmlContent)
                .WithTo([new SendContact(email, recipientName)])
                .WithCustomId(communication.CommunicationId.ToString())
                .Build();

            var response = await _client.SendTransactionalEmailAsync(emailMessage);
            var result = response.Messages.FirstOrDefault();
            var error = result?.Errors.FirstOrDefault();
            if (error is null)
            {
                _logger.LogInformation("Email {CommunicationName} sent to {Email}", template.CommunicationName, email);
            }
            else
            {
                communication.Failed(DateTimeOffset.UtcNow, error.ErrorMessage);
                _logger.LogError("Failed to send {CommunicationName} email to {Email}: {Error}", template.CommunicationName, email, error.ErrorMessage);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {CommunicationName} email to {Email}", template.CommunicationName, email);
            communication.Failed(DateTimeOffset.UtcNow, ex.Message);
        }

        await _communications.SaveChanges(communication, stoppingToken);
        return communication;
    }
}
