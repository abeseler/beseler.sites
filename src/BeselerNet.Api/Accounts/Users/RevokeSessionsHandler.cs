using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Core;
using BeselerNet.Shared.Contracts.OAuth;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users;

internal sealed class RevokeSessionsHandler
{
    private const string RefreshCookiePath = "/v1/accounts/oauth/tokens";

    public static async Task<IResult> Handle(
        RevokeSessionsRequest request,
        ClaimsPrincipal principal,
        AccountDataSource accounts,
        JwtGenerator tokens,
        TokenLogDataSource tokenLogs,
        Cookies cookies,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId))
            return TypedResults.Unauthorized();

        var account = await accounts.WithId_IncludePermissions(accountId, cancellationToken);
        if (account is null)
            return TypedResults.Unauthorized();

        if (request.Everywhere)
        {
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

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return TypedResults.BadRequest();

        var refresh = await tokens.Validate(request.RefreshToken);
        if (refresh is null
            || !int.TryParse(refresh.FindFirstValue(JwtRegisteredClaimNames.Sub), out var refreshAccount)
            || refreshAccount != accountId
            || !Guid.TryParse(refresh.FindFirstValue(JwtRegisteredClaimNames.Jti), out var jti))
        {
            return TypedResults.NoContent();
        }

        await tokenLogs.RevokeChain(jti, cancellationToken);
        return TypedResults.NoContent();
    }
}
