using BeselerNet.Shared.Core;

namespace BeselerNet.Api.Communications;

internal interface IEmailer
{
    Task<Result<Communication>> SendEmailVerification(int accountId, string email, string recipientName, string token, CancellationToken stoppingToken);
    Task<Result<Communication>> SendAccountLocked(int accountId, string email, string recipientName, CancellationToken stoppingToken);
    Task<Result<Communication>> SendPasswordReset(int accountId, string email, string recipientName, string token, CancellationToken stoppingToken);
}
