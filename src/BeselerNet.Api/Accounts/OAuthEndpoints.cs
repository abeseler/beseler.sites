using BeselerNet.Shared.Contracts.OAuth;
using System.Net.Mime;

namespace BeselerNet.Api.Accounts;

internal static class OAuthEndpoints
{
    public static void MapOAuthEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/oauth")
            .WithTags("OAuth");

        group.MapPost("/tokens", OAuthTokenRequestHandler.GenerateToken)
            .WithName("GetOAuthToken")
            .Accepts<OAuthTokenRequest>(MediaTypeNames.Application.Json)
            .Produces<OAuthTokenResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden, MediaTypeNames.Application.Json)
            .AllowAnonymous();
    }
}
