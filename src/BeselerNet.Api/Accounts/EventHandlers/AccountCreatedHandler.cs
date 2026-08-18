using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Communications;
using BeselerNet.Api.Core;
using BeselerNet.Shared;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Diagnostics;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.EventHandlers;

internal sealed class AccountCreatedHandler(JwtGenerator tokenGenerator, CommunicationService communicationService, AccountDataSource accounts) : IHandler<AccountCreated>
{
    private const string ActivityName = $"{nameof(AccountCreatedHandler)}.{nameof(Handle)}";
    private readonly JwtGenerator _tokenGenerator = tokenGenerator;
    private readonly CommunicationService _communicationService = communicationService;
    private readonly AccountDataSource _accounts = accounts;
    public async Task Handle(AccountCreated domainEvent, IEventMetadata metadata, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.Source.StartActivity(ActivityName, ActivityKind.Internal, metadata.TraceId);
        activity?.SetTag_AccountId(domainEvent.AccountId);

        if (domainEvent.Email is null)
        {
            return;
        }

        var account = await _accounts.WithId(domainEvent.AccountId, cancellationToken);
        if (account?.EmailVerifiedAt is not null)
        {
            return;
        }

        var subjectClaim = new Claim(JwtRegisteredClaimNames.Sub, domainEvent.AccountId.ToString(), ClaimValueTypes.Integer);
        var emailClaim = new Claim(JwtRegisteredClaimNames.Email, domainEvent.Email);
        var emailVerifiedClaim = new Claim(JwtRegisteredClaimNames.EmailVerified, "true", ClaimValueTypes.Boolean);
        var name = domainEvent switch
        {
            { GivenName: not null, FamilyName: not null } => $"{domainEvent.GivenName} {domainEvent.FamilyName}",
            { GivenName: not null } => domainEvent.GivenName,
            _ => domainEvent.Email
        };
        var token = _tokenGenerator.Generate(subjectClaim, AuthLimits.ConfirmEmail, [emailClaim, emailVerifiedClaim]);

        var result = await _communicationService.SendEmailVerification(domainEvent.AccountId, domainEvent.Email, name, token.AccessToken, cancellationToken);
        if (result.Failed(out var exception))
        {
            throw exception;
        }
    }
}
