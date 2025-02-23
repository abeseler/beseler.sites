using BeselerNet.Api.Accounts.Users.EndpointHandlers;
using BeselerNet.Shared.Contracts;
using BeselerNet.Shared.Contracts.Users;
using static System.Net.Mime.MediaTypeNames;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace BeselerNet.Api.Accounts.Users;

internal static class UserAccountEndpoints
{
    public static void MapUserAccountEndpoints(this IEndpointRouteBuilder builder)
    {
        var v1 = builder.MapGroup("/v1/accounts")
            .WithTags("Accounts");

        _ = v1.MapPost("/users", RegisterUserHandler.Handle)
            .WithName("RegisterUser")
            .Accepts<RegisterUserRequest>(Application.Json)
            .Produces(Status201Created)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .AllowAnonymous();

        _ = v1.MapPost("/resend-email-confirmation", ResendEmailVerificationHandler.Handle)
            .WithName("ResendEmailConfirmation")
            .Produces(Status202Accepted)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status422UnprocessableEntity, Application.Json)
            .RequireAuthorization();

        _ = v1.MapPost("/confirm-email", ConfirmEmailHandler.Handle)
            .WithName("ConfirmEmail")
            .Produces(Status204NoContent)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .RequireAuthorization();

        _ = v1.MapPost("/forgot-password", ForgotPasswordHandler.Handle)
            .WithName("SendForgotPassword")
            .Accepts<ForgotPasswordRequest>(Application.Json)
            .Produces<GenericMessageResponse>(Status202Accepted)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .ProducesProblem(Status429TooManyRequests, Application.Json)
            .AllowAnonymous();

        _ = v1.MapPost("/reset-password", ForgotPasswordHandler.Handle)
            .WithName("ResetUserPassword")
            .Accepts<ResetPasswordRequest>(Application.Json)
            .Produces(Status204NoContent)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .RequireAuthorization();
    }
}
