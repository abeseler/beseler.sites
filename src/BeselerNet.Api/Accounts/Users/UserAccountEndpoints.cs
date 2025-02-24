using BeselerNet.Api.Accounts.Users.EndpointHandlers;
using BeselerNet.Shared.Contracts;
using BeselerNet.Shared.Contracts.Users;
using static Microsoft.AspNetCore.Http.StatusCodes;
using static System.Net.Mime.MediaTypeNames;

namespace BeselerNet.Api.Accounts.Users;

internal static class UserAccountEndpoints
{
    public static void MapUserAccountEndpoints(this IEndpointRouteBuilder builder)
    {
        var v1 = builder.MapGroup("/v1/accounts")
            .WithTags("Accounts");

        _ = v1.MapPost("/users", RegisterUserHandler.Handle)
            .WithName("RegisterUser")
            .WithDescription("Register a new user account.")
            .Accepts<RegisterUserRequest>(Application.Json)
            .Produces(Status201Created)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .AllowAnonymous();

        _ = v1.MapPost("/resend-email-confirmation", ResendEmailVerificationHandler.Handle)
            .WithName("ResendEmailConfirmation")
            .WithDescription("Resend the email verification link.")
            .Produces(Status202Accepted)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status422UnprocessableEntity, Application.Json)
            .RequireAuthorization();

        _ = v1.MapPost("/confirm-email", ConfirmEmailHandler.Handle)
            .WithName("ConfirmEmail")
            .WithDescription("Confirm the email address.")
            .Produces(Status204NoContent)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .RequireAuthorization();

        _ = v1.MapPost("/forgot-password", ForgotPasswordHandler.Handle)
            .WithName("SendForgotPassword")
            .WithDescription("Send a password reset link to the email address.")
            .Accepts<ForgotPasswordRequest>(Application.Json)
            .Produces<GenericMessageResponse>(Status202Accepted)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .ProducesProblem(Status429TooManyRequests, Application.Json)
            .AllowAnonymous();

        _ = v1.MapPost("/reset-password", ResetPasswordHandler.Handle)
            .WithName("ResetUserPassword")
            .WithDescription("Reset the password.")
            .Accepts<ResetPasswordRequest>(Application.Json)
            .Produces(Status204NoContent)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .RequireAuthorization();
    }
}
