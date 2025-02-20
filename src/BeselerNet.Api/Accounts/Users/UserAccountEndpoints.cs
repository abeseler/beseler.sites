using BeselerNet.Api.Accounts.Users.EndpointHandlers;
using BeselerNet.Shared.Contracts.Users;
using System.Net.Mime;

namespace BeselerNet.Api.Accounts.Users;

internal static class UserAccountEndpoints
{
    public static void MapUserAccountEndpoints(this IEndpointRouteBuilder builder)
    {
        var v1 = builder.MapGroup("/v1/accounts")
            .WithTags("Accounts");

        v1.MapPost("/users", RegisterUserHandler.Handle)
            .WithName("RegisterUser")
            .Accepts<RegisterUserRequest>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)
            .AllowAnonymous();

        v1.MapPost("/resend-email-confirmation", ResendEmailVerificationHandler.Handle)
            .WithName("ResendEmailConfirmation")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden, MediaTypeNames.Application.Json)
            .RequireAuthorization();

        v1.MapPost("/confirm-email", ConfirmEmailHandler.Handle)
            .WithName("ConfirmEmail")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden, MediaTypeNames.Application.Json)
            .RequireAuthorization();

        v1.MapPost("/forgot-password", ForgotPasswordHandler.Handle)
            .WithName("SendForgotPassword")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)
            .AllowAnonymous();

        v1.MapPost("/reset-password", ForgotPasswordHandler.Handle)
            .WithName("ResetUserPassword")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden, MediaTypeNames.Application.Json)
            .RequireAuthorization();
    }
}
