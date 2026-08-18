using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Core;
using BeselerNet.Shared.Contracts.OAuth;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users;

internal sealed class ConfirmEmailHandler
{
    private const string RefreshCookiePath = "/v1/accounts/oauth/tokens";

    public static async Task<IResult> Handle(
        ClaimsPrincipal principal,
        AccountDataSource accounts,
        JwtGenerator tokens,
        TokenLogDataSource tokenLogs,
        Cookies cookies,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId)
            || principal.FindFirstValue(JwtRegisteredClaimNames.Email) is not { } email
            || principal.FindFirstValue(JwtRegisteredClaimNames.EmailVerified) is null)
        {
            return TypedResults.Unauthorized();
        }

        var account = await accounts.WithId_IncludePermissions(accountId, cancellationToken);
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
            _ => null
        };

        if (problem is not null)
        {
            return TypedResults.Problem(problem);
        }

        if (account!.EmailVerifiedAt is null)
        {
            account.VerifyEmail(email);
            await accounts.SaveChanges(account, cancellationToken);
        }

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
