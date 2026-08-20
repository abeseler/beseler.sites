using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Core;
using BeselerNet.Shared.Contracts.OAuth;
using BeselerNet.Shared.Contracts.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users;

internal static class ChangePasswordHandler
{
    private const string RefreshCookiePath = "/v1/accounts/oauth/tokens";

    public static async Task<IResult> Handle(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        AccountDataSource accounts,
        IPasswordHasher<Account> passwordHasher,
        JwtGenerator tokens,
        TokenLogDataSource tokenLogs,
        Cookies cookies,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId))
            return TypedResults.Unauthorized();

        if (request.IsInvalid(out var errors))
            return TypedResults.ValidationProblem(errors);

        var account = await accounts.WithId_IncludePermissions(accountId, cancellationToken);
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
        await tokenLogs.RevokeAll(target.AccountId, cancellationToken);

        var tokenResult = tokens.Generate(target.ToClaimsPrincipal());
        if (tokenResult.RefreshToken is not null)
        {
            var log = TokenLog.Create(tokenResult, target.AccountId);
            await tokenLogs.SaveChanges(log, cancellationToken);
            cookies.Set(Cookies.RefreshToken, tokenResult.RefreshToken, new()
            {
                Expires = tokenResult.RefreshTokenExpires,
                SameSite = SameSiteMode.Strict,
                Secure = true,
                HttpOnly = true,
                Path = RefreshCookiePath
            });
        }

        return TypedResults.Ok(new OAuthTokenResponse
        {
            AccessToken = tokenResult.AccessToken,
            TokenType = "Bearer",
            ExpiresIn = tokenResult.ExpiresIn,
            RefreshToken = tokenResult.RefreshToken
        });
    }
}
