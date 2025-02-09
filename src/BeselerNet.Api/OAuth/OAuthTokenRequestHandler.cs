using BeselerNet.Shared.Contracts;
using System.Diagnostics;

namespace BeselerNet.Api.OAuth;

internal static class OAuthTokenRequestHandler
{
    public static async Task<IResult> GenerateToken(OAuthTokenRequest request, HttpResponse httpResponse)
    {
        if (!request.IsValid(out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        return request.GrantType switch
        {
            OAuthGrantType.password => await HandlePasswordGrant(request, httpResponse),
            OAuthGrantType.client_credentials => await HandleClientCredentialsGrant(request, httpResponse),
            OAuthGrantType.refresh_token => await HandleRefreshTokenGrant(request, httpResponse),
            _ => throw new UnreachableException()
        };
    }

    private static Task<IResult> HandlePasswordGrant(OAuthTokenRequest request, HttpResponse httpResponse)
    {
        throw new NotImplementedException();
    }

    private static Task<IResult> HandleClientCredentialsGrant(OAuthTokenRequest request, HttpResponse httpResponse)
    {
        throw new NotImplementedException();
    }

    private static Task<IResult> HandleRefreshTokenGrant(OAuthTokenRequest request, HttpResponse httpResponse)
    {
        throw new NotImplementedException();
    }
}
