using BeselerNet.Shared.Contracts.OAuth;
using System.Net.Mime;

namespace BeselerNet.Api.Accounts.OAuth;

internal static class OAuthEndpoints
{
    public static void MapOAuthEndpoints(this IEndpointRouteBuilder builder)
    {
        var v1 = builder.MapGroup("/v1/accounts/oauth")
            .WithTags("OAuth");

        v1.MapPost("/tokens", CreateTokenHandler.Handle)
            .WithName("GetOAuthToken")
            .Accepts<OAuthTokenRequest>(MediaTypeNames.Application.Json)
            .Produces<OAuthTokenResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden, MediaTypeNames.Application.Json)
            .AllowAnonymous();
    }
}
