using BeselerNet.Shared.Contracts.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users;

internal static class ChangePasswordHandler
{
    public static async Task<IResult> Handle(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        AccountDataSource accounts,
        IPasswordHasher<Account> passwordHasher,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId))
            return TypedResults.Unauthorized();

        if (request.IsInvalid(out var errors))
            return TypedResults.ValidationProblem(errors);

        var account = await accounts.WithId(accountId, cancellationToken);
        var problem = account switch
        {
            null => AccountProblems.NotFound,
            { IsDisabled: true } => AccountProblems.Disabled,
            { IsLocked: true } => AccountProblems.Locked,
            _ => null
        };

        if (problem is not null)
            return TypedResults.Problem(problem);

        var target = account!;
        var verified = passwordHasher.VerifyHashedPassword(target, target.SecretHash, request.CurrentPassword);
        if (verified is PasswordVerificationResult.Failed)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["current_password"] = ["Current password is wrong."]
            });
        }

        target.ChangePassword(passwordHasher.HashPassword(target, request.Password));
        await accounts.SaveChanges(target, cancellationToken);
        return TypedResults.NoContent();
    }
}
