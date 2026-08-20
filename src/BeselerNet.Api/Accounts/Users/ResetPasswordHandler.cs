using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Core;
using BeselerNet.Shared.Contracts.OAuth;
using BeselerNet.Shared.Contracts.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users;

internal sealed class ResetPasswordHandler
{
    public const string ResetClaim = "reset";
    private const string RefreshCookiePath = "/v1/accounts/oauth/tokens";

    public static async Task<IResult> Handle(
        ResetPasswordRequest request,
        ClaimsPrincipal principal,
        AccountDataSource accounts,
        IPasswordHasher<Account> passwordHasher,
        JwtGenerator tokens,
        TokenLogDataSource tokenLogs,
        Cookies cookies,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId)
            || principal.FindFirstValue(ResetClaim) is null)
        {
            return TypedResults.Unauthorized();
        }

        if (request.IsInvalid(out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        var account = await accounts.WithId_IncludePermissions(accountId, cancellationToken);
        var problem = account switch
        {
            null => new()
            {
                Title = "Invalid Password Reset",
                Detail = "The password reset token is invalid.",
                Status = StatusCodes.Status403Forbidden
            },
            { IsDisabled: true } => AccountProblems.Disabled,
            _ => null
        };

        if (problem is not null)
        {
            return TypedResults.Problem(problem);
        }

        var hashedPassword = passwordHasher.HashPassword(account!, request.Password!);
        account!.ChangePassword(hashedPassword);
        await accounts.SaveChanges(account, cancellationToken);
        await tokenLogs.RevokeAll(account.AccountId, cancellationToken);

        var tokenResult = tokens.Generate(account.ToClaimsPrincipal());
        if (tokenResult.RefreshToken is not null)
        {
            var log = TokenLog.Create(tokenResult, account.AccountId);
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
