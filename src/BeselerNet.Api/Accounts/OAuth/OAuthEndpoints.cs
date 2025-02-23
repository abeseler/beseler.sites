using BeselerNet.Shared.Contracts.OAuth;
using static System.Net.Mime.MediaTypeNames;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace BeselerNet.Api.Accounts.OAuth;

internal static class OAuthEndpoints
{
    public static void MapOAuthEndpoints(this IEndpointRouteBuilder builder)
    {
        var v1 = builder.MapGroup("/v1/accounts/oauth")
            .WithTags("OAuth");

        _ = v1.MapPost("/tokens", CreateTokenHandler.Handle)
            .WithName("GetOAuthToken")
            .WithDescription("Get OAuth token")
            .Accepts<OAuthTokenRequest>(Application.Json)
            .Produces<OAuthTokenResponse>(Status200OK, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .AllowAnonymous();
    }
}
