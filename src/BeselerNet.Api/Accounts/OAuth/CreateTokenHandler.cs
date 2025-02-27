using BeselerNet.Api.Core;
using BeselerNet.Shared.Contracts.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Diagnostics;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.OAuth;

internal static class CreateTokenHandler
{
    private const string RefreshCookiePath = "/v1/accounts/oauth/tokens";
    private const string AccessTokenActivityName = $"{nameof(CreateTokenHandler)}.{nameof(HandleAccessTokenGrant)}";
    private const string RefreshTokenActivityName = $"{nameof(CreateTokenHandler)}.{nameof(HandleRefreshTokenGrant)}";
    public readonly struct Parameters
    {
        public OAuthTokenRequest Request { get; init; }
        public AccountDataSource Accounts { get; init; }
        public IPasswordHasher<Account> PasswordHasher { get; init; }
        public JwtGenerator TokenGenerator { get; init; }
        public TokenLogDataSource TokenLogs { get; init; }
        public Cookies Cookies { get; init; }
        public CancellationToken StoppingToken { get; init; }
    }

    public static async Task<IResult> Handle([AsParameters] Parameters parameters)
    {
        if (parameters.Request.HasValidationErrors(out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        return parameters.Request.GrantType switch
        {
            OAuthGrantType.password => await HandleAccessTokenGrant(parameters.Request.Username!, parameters.Request.Password!, parameters),
            OAuthGrantType.client_credentials => await HandleAccessTokenGrant(parameters.Request.ClientId!, parameters.Request.ClientSecret!, parameters),
            OAuthGrantType.refresh_token => await HandleRefreshTokenGrant(parameters),
            _ => throw new UnreachableException("Oauth request with invalid grant_type.")
        };
    }

    private static async Task<IResult> HandleAccessTokenGrant(string username, string secret, Parameters parameters)
    {
        using var activity = Telemetry.Source.StartActivity(AccessTokenActivityName, ActivityKind.Internal);
        if (await parameters.Accounts.WithUsername_IncludePermissions(username, parameters.StoppingToken) is not { } account)
        {
            return TypedResults.Unauthorized();
        }

        activity?.SetTag_AccountId(account.AccountId);
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

        var result = parameters.PasswordHasher.VerifyHashedPassword(account, account.SecretHash, secret);
        if (result is PasswordVerificationResult.Failed)
        {
            account.FailLogin();
            await parameters.Accounts.SaveChanges(account, parameters.StoppingToken);
            return TypedResults.Unauthorized();
        }
        else if (result is PasswordVerificationResult.SuccessRehashNeeded)
        {
            var hash = parameters.PasswordHasher.HashPassword(account, parameters.Request.Password!);
            account.ChangePassword(hash);
        }

        account.Login();
        await parameters.Accounts.SaveChanges(account, parameters.StoppingToken);

        var principal = account.ToClaimsPrincipal();
        var tokenResult = parameters.TokenGenerator.Generate(principal);

        if (tokenResult.RefreshToken is not null)
        {
            var log = TokenLog.Create(tokenResult, account.AccountId);
            await parameters.TokenLogs.SaveChanges(log, parameters.StoppingToken);

            parameters.Cookies.Set(Cookies.RefreshToken, tokenResult.RefreshToken, new()
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

    private static async Task<IResult> HandleRefreshTokenGrant(Parameters parameters)
    {
        using var activity = Telemetry.Source.StartActivity(RefreshTokenActivityName, ActivityKind.Internal);
        var token = parameters.Request.RefreshToken ?? parameters.Cookies.Get(Cookies.RefreshToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return TypedResults.Unauthorized();
        }

        if (await parameters.TokenGenerator.Validate(token) is not { } principal
            || !int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId)
            || !Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Jti), out var jti)
            || await parameters.Accounts.WithId_IncludePermissions(accountId, parameters.StoppingToken) is not { } account)
        {
            return TypedResults.Unauthorized();
        }

        activity?.SetTag_AccountId(accountId);
        var log = await parameters.TokenLogs.WithJti(jti, parameters.StoppingToken);
        if (log is { ReplacedBy: not null })
        {
            await parameters.TokenLogs.RevokeChain(jti, parameters.StoppingToken);
            return TypedResults.Unauthorized();
        }
        else if (log is null or { RevokedAt: not null })
        {
            return TypedResults.Unauthorized();
        }

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

        principal = account.ToClaimsPrincipal();
        var tokenResult = parameters.TokenGenerator.Generate(principal);
        var newLog = TokenLog.Create(tokenResult, account.AccountId);
        log?.ReplaceWith(newLog.Jti);

        await parameters.TokenLogs.SaveChanges(newLog, parameters.StoppingToken);
        if (log is not null)
        {
            await parameters.TokenLogs.SaveChanges(log, parameters.StoppingToken);
        }

        var response = new OAuthTokenResponse
        {
            AccessToken = tokenResult.AccessToken,
            TokenType = "Bearer",
            ExpiresIn = tokenResult.ExpiresIn,
            RefreshToken = tokenResult.RefreshToken
        };

        if (tokenResult.RefreshToken is not null)
        {
            parameters.Cookies.Set(Cookies.RefreshToken, tokenResult.RefreshToken, new()
            {
                Expires = tokenResult.RefreshTokenExpires,
                SameSite = SameSiteMode.Strict,
                Secure = true,
                HttpOnly = true,
                Path = RefreshCookiePath
            });
        }

        return TypedResults.Ok(response);
    }
}
