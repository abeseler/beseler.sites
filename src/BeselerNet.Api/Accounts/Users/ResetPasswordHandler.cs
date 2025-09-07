using BeselerNet.Shared.Contracts.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users;

internal sealed class ResetPasswordHandler
{
    public static async Task<IResult> Handle(ResetPasswordRequest request, ClaimsPrincipal principal, AccountDataSource accounts, IPasswordHasher<Account> passwordHasher, CancellationToken cancellationToken)
    {
        if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId))
        {
            return TypedResults.Unauthorized();
        }

        var account = await accounts.WithId(accountId, cancellationToken);
        var problem = account switch
        {
            { IsDisabled: true } => AccountProblems.Disabled,
            { IsLocked: true } => AccountProblems.Locked,
            _ => null
        };

        if (problem is not null)
        {
            return TypedResults.Problem(problem);
        }

        if (request.HasValidationErrors(out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        var hashedPassword = passwordHasher.HashPassword(account!, request.Password!);
        account!.ChangePassword(hashedPassword);

        await accounts.SaveChanges(account, cancellationToken);

        return TypedResults.NoContent();
    }
}
