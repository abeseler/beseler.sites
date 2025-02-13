using BeselerNet.Api.Core;
using BeselerNet.Shared.Contracts;
using System.Diagnostics;

namespace BeselerNet.Api.Accounts;

internal static class OAuthTokenRequestHandler
{
    public readonly struct RequestParameters
    {
        public OAuthTokenRequest Request { get; init; }
        public AccountDataSource Accounts { get; init; }
        public Cookies Cookies { get; init; }
        public CancellationToken StoppingToken { get; init; }
    }

    public static async Task<IResult> GenerateToken([AsParameters] RequestParameters parameters)
    {
        if (parameters.Request.HasValidationErrors(out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        return parameters.Request.GrantType switch
        {
            OAuthGrantType.password => await HandlePasswordGrant(parameters),
            OAuthGrantType.client_credentials => await HandleClientCredentialsGrant(parameters),
            OAuthGrantType.refresh_token => await HandleRefreshTokenGrant(parameters),
            _ => throw new UnreachableException()
        };
    }

    private static async Task<IResult> HandlePasswordGrant(RequestParameters parameters)
    {
        var account = await parameters.Accounts.WithEmail(parameters.Request.Username!, parameters.StoppingToken);
        if (account is not { })
        {
            return TypedResults.Unauthorized();
        }

        throw new NotImplementedException();
    }

    private static Task<IResult> HandleClientCredentialsGrant(RequestParameters parameters)
    {
        throw new NotImplementedException();
    }

    private static async Task<IResult> HandleRefreshTokenGrant(RequestParameters parameters)
    {
        var token = parameters.Request.RefreshToken ?? parameters.Cookies.Get("refresh_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            return TypedResults.Unauthorized();
        }

        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}
