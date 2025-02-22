using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users.EndpointHandlers;

internal sealed class ConfirmEmailHandler
{
    public static async Task<IResult> Handle(ClaimsPrincipal principal, AccountDataSource accounts, CancellationToken stoppingToken)
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
            { EmailVerifiedAt: not null } => AccountProblems.EmailAlreadyVerified,
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
