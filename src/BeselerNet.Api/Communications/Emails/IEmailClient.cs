namespace BeselerNet.Api.Communications.Emails;

internal interface IEmailClient
{
    string ProviderName { get; }
    Task Send(Communication communication, EmailTemplate template, string email, string recipientName, CancellationToken cancellationToken);
}
