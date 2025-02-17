using BeselerNet.Api.Core;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Diagnostics;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.EventHandlers;

internal sealed class AccountCreatedHandler(JwtGenerator tokenGenerator, EmailService emailer)
{
    private readonly JwtGenerator _tokenGenerator = tokenGenerator;
    private readonly EmailService _emailer = emailer;
    public async Task Handle(AccountCreated domainEvent, CancellationToken stoppingToken)
    {
        using var activity = Telemetry.Source.StartActivity("AccountCreatedHandler.Handle", ActivityKind.Internal, domainEvent.TraceId);
        activity?.SetTag_AccountId(domainEvent.AccountId);

        if (domainEvent.Email is null)
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
        var token = _tokenGenerator.Generate(subjectClaim, TimeSpan.FromMinutes(10), [emailClaim, emailVerifiedClaim]);

        await _emailer.SendEmailVerification(domainEvent.Email, name, token.AccessToken, stoppingToken);
    }
}
