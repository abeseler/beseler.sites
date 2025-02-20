using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Core;
using BeselerNet.Shared.Contracts.Users;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users.EndpointHandlers;

internal sealed class ForgotPasswordHandler
{
    public static async Task<IResult> Handle(ForgotPasswordRequest request, AccountDataSource accounts, JwtGenerator tokens, EmailService emailer, CancellationToken stoppingToken)
    {
        var account = await accounts.WithEmail(request.Email, stoppingToken);
        if (account is not null && account is { IsDisabled: false })
        {
            var subjectClaim = new Claim(JwtRegisteredClaimNames.Sub, account.AccountId.ToString(), ClaimValueTypes.Integer);
            var token = tokens.Generate(subjectClaim, TimeSpan.FromMinutes(20), [new("ResetPassword", "true", ClaimValueTypes.Boolean)]);

            await emailer.SendPasswordReset(request.Email, account.Name, token.AccessToken, stoppingToken);
        }

        return TypedResults.Accepted((string?)null);
    }
}
