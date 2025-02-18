using BeselerNet.Api.Core;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users;

internal static class EmailVerificationHandlers
{
    public static async Task<IResult> CreateEmailVerification(ClaimsPrincipal principal, AccountDataSource accounts, JwtGenerator tokens, EmailService emailer, CancellationToken stoppingToken)
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
            { EmailVerifiedOn: not null } => AccountProblems.EmailAlreadyVerified,
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

    public static async Task<IResult> ConfirmEmailVerification(ClaimsPrincipal principal, AccountDataSource accounts, CancellationToken stoppingToken)
    {
        if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId)
            || principal.FindFirstValue(JwtRegisteredClaimNames.Email) is not { } email
            || principal.FindFirstValue(JwtRegisteredClaimNames.EmailVerified) is null)
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
            { Email: var e } when e != email => new()
            {
                Title = "Invalid Email Verification",
                Detail = "The email verification token is invalid.",
                Status = StatusCodes.Status403Forbidden
            },
            { IsDisabled: true } => AccountProblems.Disabled,
            { IsLocked: true } => AccountProblems.Locked,
            { EmailVerifiedOn: not null } => AccountProblems.EmailAlreadyVerified,
            _ => null
        };

        if (problem is not null)
        {
            return TypedResults.Problem(problem);
        }

        account!.VerifyEmail(email);
        await accounts.SaveChanges(account, stoppingToken);

        return TypedResults.NoContent();
    }
}
