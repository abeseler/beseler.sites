using BeselerNet.Api.Core;
using System.Diagnostics;

namespace BeselerNet.Api.Accounts.EventHandlers;

internal sealed class AccountLoginFailedHandler(AccountDataSource accounts, EmailService emailer)
{
    private readonly AccountDataSource _accounts = accounts;
    private readonly EmailService _emailer = emailer;
    public async Task Handle(AccountLoginFailed domainEvent, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.Source.StartActivity("AccountLoginFailedHandler.Handle", ActivityKind.Internal, domainEvent.TraceId);
        activity?.SetTag_AccountId(domainEvent.AccountId);

        var account = await _accounts.WithId(domainEvent.AccountId, cancellationToken);
        if (account is { Email: not null, IsLocked: true })
        {
            await _emailer.SendAccountLocked(account.Email, account.Name, cancellationToken);
        }
    }
}
