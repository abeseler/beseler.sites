using BeselerNet.Api.Accounts;
using BeselerNet.Api.Communications;
using BeselerNet.Api.Core;
using System.Diagnostics;

namespace BeselerNet.Api.Events.Handlers;

internal sealed class AccountLoginFailedHandler(AccountDataSource accounts, EmailerProvider emailerProvider)
{
    private readonly AccountDataSource _accounts = accounts;
    private readonly IEmailer _emailer = emailerProvider.GetEmailer();
    public async Task Handle(AccountLoginFailed domainEvent, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.Source.StartActivity("AccountLoginFailedHandler.Handle", ActivityKind.Internal, domainEvent.TraceId);
        activity?.SetTag_AccountId(domainEvent.AccountId);

        var account = await _accounts.WithId(domainEvent.AccountId, cancellationToken);
        if (account is { Email: not null, IsLocked: true })
        {
            var result = await _emailer.SendAccountLocked(account.AccountId, account.Email, account.Name, cancellationToken);
            if (result.Failed(out var exception))
            {
                throw exception;
            }
        }
    }
}
