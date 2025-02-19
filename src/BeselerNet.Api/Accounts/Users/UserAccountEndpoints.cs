using BeselerNet.Shared.Contracts.Users;
using System.Net.Mime;

namespace BeselerNet.Api.Accounts.Users;

internal static class UserAccountEndpoints
{
    public static void MapUserAccountEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/accounts")
            .WithTags("Accounts");

        group.MapPost("/users", UserEndpointHandlers.RegisterUser)
            .WithName("RegisterUser")
            .Accepts<RegisterUserRequest>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        group.MapPost("/verify-email", EmailVerificationHandlers.CreateEmailVerification)
            .WithName("CreateEmailVerification")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden, MediaTypeNames.Application.Json)
            .RequireAuthorization();

        group.MapPut("/verify-email", EmailVerificationHandlers.ConfirmEmailVerification)
            .WithName("ConfirmEmailVerification")
            .RequireAuthorization();

        group.MapPost("/password-reset", PasswordResetHandlers.CreatePasswordReset)
            .WithName("CreatePasswordReset")
            .AllowAnonymous();

        group.MapPut("/password-reset", PasswordResetHandlers.ConfirmPasswordReset)
            .WithName("ConfirmPasswordReset")
            .RequireAuthorization();
    }
}
