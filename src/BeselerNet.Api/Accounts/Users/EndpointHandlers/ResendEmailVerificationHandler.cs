using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Core;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users.EndpointHandlers;

internal sealed class ResendEmailVerificationHandler
{
    public static async Task<IResult> Handle(ClaimsPrincipal principal, AccountDataSource accounts, JwtGenerator tokens, EmailService emailer, CancellationToken stoppingToken)
    {
        if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId))
        {
            return TypedResults.Unauthorized();
        }
        var account = await accounts.WithId(accountId, stoppingToken);
        var problem = account switch
        {
            null or { Email: null } => new()
            {
                Title = "Invalid Email Verification",
                Detail = "The email verification token is invalid.",
                Status = StatusCodes.Status403Forbidden
            },
            { IsDisabled: true } => AccountProblems.Disabled,
            { IsLocked: true } => AccountProblems.Locked,
            { EmailVerifiedAt: not null } => AccountProblems.EmailAlreadyVerified,
            _ => null
        };

        if (problem is not null)
        {
            return TypedResults.Problem(problem);
        }

        var subjectClaim = new Claim(JwtRegisteredClaimNames.Sub, account!.AccountId.ToString(), ClaimValueTypes.Integer);
        var emailClaim = new Claim(JwtRegisteredClaimNames.Email, account.Email!);
        var emailVerifiedClaim = new Claim(JwtRegisteredClaimNames.EmailVerified, "true", ClaimValueTypes.Boolean);

        var token = tokens.Generate(subjectClaim, TimeSpan.FromMinutes(10), [emailClaim, emailVerifiedClaim]);

        await emailer.SendEmailVerification(account.Email!, account.Name, token.AccessToken, stoppingToken);
        return TypedResults.NoContent();
    }
}
