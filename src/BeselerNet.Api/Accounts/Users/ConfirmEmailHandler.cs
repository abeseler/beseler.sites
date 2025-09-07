using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users;

internal sealed class ConfirmEmailHandler
{
    public static async Task<IResult> Handle(ClaimsPrincipal principal, AccountDataSource accounts, CancellationToken cancellationToken)
    {
        if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId)
            || principal.FindFirstValue(JwtRegisteredClaimNames.Email) is not { } email
            || principal.FindFirstValue(JwtRegisteredClaimNames.EmailVerified) is null)
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
            { Email: var e } when e != email => new()
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

        account!.VerifyEmail(email);
        await accounts.SaveChanges(account, cancellationToken);

        return TypedResults.NoContent();
    }
}
