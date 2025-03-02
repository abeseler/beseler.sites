﻿using BeselerNet.Api.Accounts;
using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Communications;
using BeselerNet.Api.Core;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Diagnostics;
using System.Security.Claims;

namespace BeselerNet.Api.Events.Handlers;

internal sealed class AccountCreatedHandler(JwtGenerator tokenGenerator, EmailerProvider emailerProvider)
{
    private readonly JwtGenerator _tokenGenerator = tokenGenerator;
    private readonly IEmailer _emailer = emailerProvider.GetEmailer();
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

        var result = await _emailer.SendEmailVerification(domainEvent.AccountId, domainEvent.Email, name, token.AccessToken, stoppingToken);
        if (result.Failed(out var exception))
        {
            throw exception;
        }
    }
}
