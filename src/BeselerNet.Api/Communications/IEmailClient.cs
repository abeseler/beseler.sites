namespace BeselerNet.Api.Communications;

internal interface IEmailClient
{
    Task<Communication> Send(EmailTemplate template, int accountId, string email, string recipientName, CancellationToken cancellationToken);
}
