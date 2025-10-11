using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Communications;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users;

internal sealed class ResendEmailVerificationHandler
{
    public static async Task<IResult> Handle(ClaimsPrincipal principal, AccountDataSource accounts, JwtGenerator tokens, Emailer emailer, CancellationToken cancellationToken)
    {
        if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId))
        {
            return TypedResults.Unauthorized();
        }
        var account = await accounts.WithId(accountId, cancellationToken);
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
            { EmailVerifiedAt: not null } => AccountProblems.EmailAlreadyVerified(account.Email),
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

        var result = await emailer.SendEmailVerification(account.AccountId, account.Email!, account.Name, token.AccessToken, cancellationToken);

        return result.Match<IResult>(
            _ => TypedResults.NoContent(),
            exception => TypedResults.Problem(new()
            {
                Title = "Email Verification Send Failed",
                Detail = "The email verification could not be sent. Please try again later.",
                Status = StatusCodes.Status422UnprocessableEntity
            })
        );
    }
}
