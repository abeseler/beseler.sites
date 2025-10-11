using BeselerNet.Api.Communications;
using BeselerNet.Api.Core;
using System.Diagnostics;

namespace BeselerNet.Api.Accounts.EventHandlers;

internal sealed class AccountLoginFailedHandler(AccountDataSource accounts, CommunicationService communicationService) : IHandler<AccountLoginFailed>
{
    private readonly AccountDataSource _accounts = accounts;
    private readonly CommunicationService _communicationService = communicationService;
    public async Task Handle(AccountLoginFailed domainEvent, IEventMetadata metadata, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.Source.StartActivity("AccountLoginFailedHandler.Handle", ActivityKind.Internal, metadata.TraceId);
        activity?.SetTag_AccountId(domainEvent.AccountId);

        var account = await _accounts.WithId(domainEvent.AccountId, cancellationToken);
        if (account is { Email: not null, IsLocked: true })
        {
            var result = await _communicationService.SendAccountLocked(account.AccountId, account.Email, account.Name, cancellationToken);
            if (result.Failed(out var exception))
            {
                throw exception;
            }
        }
    }
}
