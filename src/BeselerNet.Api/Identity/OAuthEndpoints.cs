using BeselerNet.Api.Identity.EndpointHandlers;
using BeselerNet.Shared.Contracts;
using System.Net.Mime;

namespace BeselerNet.Api.Identity;

internal static class OAuthEndpoints
{
    public static void MapOAuthEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/oauth")
            .WithTags("OAuth");

        group.MapPost("/token", OAuthTokenRequestHandler.GenerateToken)
            .WithName("GetOAuthToken")
            .Accepts<OAuthTokenRequest>(MediaTypeNames.Application.Json)
            .Produces<OAuthTokenResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
