using BeselerNet.Api.Communications.Emails;
using BeselerNet.Shared.Core;
using Microsoft.Extensions.Options;

namespace BeselerNet.Api.Communications;

internal sealed class CommunicationService(CommunicationDataSource dataSource, IOptions<CommunicationOptions> options, IEmailClient emailClient, ILogger<CommunicationService> logger)
{
    private readonly CommunicationDataSource _dataSource = dataSource;
    private readonly CommunicationOptions _options = options.Value;
    private readonly IEmailClient _emailClient = emailClient;
    private readonly ILogger<CommunicationService> _logger = logger;

    public async Task<Result<Communication>> SendEmailVerification(int accountId, string email, string recipientName, string token, CancellationToken cancellationToken)
    {
        var template = EmailTemplates.EmailVerification(_options, token);
        var communication = await SendEmail(accountId, template, email, recipientName, cancellationToken);
        if (communication.FailedAt.HasValue)
        {
            _logger.LogInformation("Token not sent: {Token}", token);
        }
        return communication;
    }

    public async Task<Result<Communication>> SendAccountLocked(int accountId, string email, string recipientName, CancellationToken cancellationToken)
    {
        var template = EmailTemplates.AccountLocked(_options, recipientName);
        var communication = await SendEmail(accountId, template, email, recipientName, cancellationToken);
        return communication;
    }

    public async Task<Result<Communication>> SendPasswordReset(int accountId, string email, string recipientName, string token, CancellationToken cancellationToken)
    {
        var template = EmailTemplates.PasswordReset(_options, recipientName, token);
        var communication = await SendEmail(accountId, template, email, recipientName, cancellationToken);
        if (communication.FailedAt.HasValue)
        {
            _logger.LogInformation("Token not sent: {Token}", token);
        }
        return communication;
    }

    private async Task<Communication> SendEmail(int accountId, EmailTemplate template, string recipientEmail, string recipientName, CancellationToken ct)
    {
        var communication = Communication.Create(_emailClient.ProviderName, CommunicationType.Email, template.CommunicationName, accountId);
        try
        {
            await _emailClient.Send(communication, template, recipientEmail, recipientName, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {CommunicationName} email to {Email}", template.CommunicationName, recipientEmail);
            communication.Failed(DateTimeOffset.UtcNow, ex.Message);
        }
        finally
        {
            await _dataSource.SaveChanges(communication, ct);
        }

        return communication;
    }
}
