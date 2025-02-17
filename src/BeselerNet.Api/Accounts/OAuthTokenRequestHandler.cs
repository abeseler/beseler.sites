using BeselerNet.Api.Core;
using BeselerNet.Shared.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Diagnostics;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts;

internal static class OAuthTokenRequestHandler
{
    public readonly struct Parameters
    {
        public OAuthTokenRequest Request { get; init; }
        public AccountDataSource Accounts { get; init; }
        public IPasswordHasher<Account> PasswordHasher { get; init; }
        public JwtGenerator TokenGenerator { get; init; }
        public Cookies Cookies { get; init; }
        public CancellationToken StoppingToken { get; init; }
    }

    public static async Task<IResult> GenerateToken([AsParameters] Parameters parameters)
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
        var account = await parameters.Accounts.WithUsername(username, parameters.StoppingToken);
        if (account is not { SecretHash: not null })
        {
            return TypedResults.Unauthorized();
        }
        else if (account.IsDisabled)
        {
            return TypedResults.Problem(new()
            {
                Title = "Account Disabled",
                Detail = "Your account has been disabled. Please contact support.",
                Status = StatusCodes.Status403Forbidden
            });
        }
        else if (account.IsLocked)
        {
            return TypedResults.Problem(new()
            {
                Title = "Account Locked",
                Detail = "Your account has been locked due to too many failed login attempts. Please contact support.",
                Status = StatusCodes.Status403Forbidden
            });
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
            account.ResetPassword(hash);
        }

        account.Login();
        await parameters.Accounts.SaveChanges(account, parameters.StoppingToken);

        var principal = account.ToClaimsPrincipal();
        var tokenResult = parameters.TokenGenerator.Generate(principal);

        // TODO: Save refresh token in database

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
                Path = "/oauth/token"
            });
        }

        return TypedResults.Ok(response);
    }

    private static async Task<IResult> HandleRefreshTokenGrant(Parameters parameters)
    {
        var token = parameters.Request.RefreshToken ?? parameters.Cookies.Get(Cookies.RefreshToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return TypedResults.Unauthorized();
        }
        var principal = await parameters.TokenGenerator.Validate(token);
        if (principal is null
            || !int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId)
            || !Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Jti), out var refreshTokenId))
        {
            return TypedResults.Unauthorized();
        }
        var account = await parameters.Accounts.WithId(accountId, parameters.StoppingToken);
        if (account is null)
        {
            return TypedResults.Unauthorized();
        }
        else if (account.IsDisabled)
        {
            return TypedResults.Problem(new()
            {
                Title = "Account Disabled",
                Detail = "Your account has been disabled. Please contact support.",
                Status = StatusCodes.Status403Forbidden
            });
        }
        else if (account.IsLocked)
        {
            return TypedResults.Problem(new()
            {
                Title = "Account Locked",
                Detail = "Your account has been locked due to too many failed login attempts. Please contact support.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        principal = account.ToClaimsPrincipal();
        var tokenResult = parameters.TokenGenerator.Generate(principal);

        // TODO: Save refresh token in database

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
                Path = "/oauth/token"
            });
        }

        return TypedResults.Ok(response);
    }
}
