using BeselerNet.Shared.Core;
using Microsoft.Extensions.Options;

namespace BeselerNet.Api.Communications;

internal sealed class Emailer(IEmailClient emailClient, IOptions<CommunicationOptions> options, ILogger<Emailer> logger)
{
    private readonly IEmailClient _client = emailClient;
    private readonly CommunicationOptions _options = options.Value;
    private readonly ILogger<Emailer> _logger = logger;

    public async Task<Result<Communication>> SendEmailVerification(int accountId, string email, string recipientName, string token, CancellationToken cancellationToken)
    {
        var template = EmailTemplates.EmailVerification(_options.ConfirmEmailUrl!, token);
        var communication = await _client.Send(template, accountId, email, recipientName, cancellationToken);
        if (communication.FailedAt.HasValue)
        {
            _logger.LogInformation("Token not sent: {Token}", token);
        }
        return communication;
    }

    public async Task<Result<Communication>> SendAccountLocked(int accountId, string email, string recipientName, CancellationToken cancellationToken)
    {
        var template = EmailTemplates.AccountLocked(recipientName);
        return await _client.Send(template, accountId, email, recipientName, cancellationToken);
    }

    public async Task<Result<Communication>> SendPasswordReset(int accountId, string email, string recipientName, string token, CancellationToken cancellationToken)
    {
        var template = EmailTemplates.PasswordReset(recipientName, _options.ResetPasswordUrl!, token);
        var communication = await _client.Send(template, accountId, email, recipientName, cancellationToken);
        if (communication.FailedAt.HasValue)
        {
            _logger.LogInformation("Token not sent: {Token}", token);
        }
        return communication;
    }
}
