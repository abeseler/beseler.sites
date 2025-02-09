using BeselerNet.Api.Core;
using BeselerNet.Shared.Contracts;
using System.Diagnostics;

namespace BeselerNet.Api.OAuth;

internal static class OAuthTokenRequestHandler
{
    public static async Task<IResult> GenerateToken(OAuthTokenRequest request, Cookies cookies, CancellationToken stoppingToken)
    {
        if (!request.IsValid(out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        return request.GrantType switch
        {
            OAuthGrantType.password => await HandlePasswordGrant(request, cookies),
            OAuthGrantType.client_credentials => await HandleClientCredentialsGrant(request, cookies),
            OAuthGrantType.refresh_token => await HandleRefreshTokenGrant(request, cookies),
            _ => throw new UnreachableException()
        };
    }

    private static Task<IResult> HandlePasswordGrant(OAuthTokenRequest request, Cookies cookies)
    {
        throw new NotImplementedException();
    }

    private static Task<IResult> HandleClientCredentialsGrant(OAuthTokenRequest request, Cookies cookies)
    {
        throw new NotImplementedException();
    }

    private static Task<IResult> HandleRefreshTokenGrant(OAuthTokenRequest request, Cookies cookies)
    {
        throw new NotImplementedException();
    }
}
