using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Accounts.Users;
using BeselerNet.Shared.Contracts;
using BeselerNet.Shared.Contracts.OAuth;
using BeselerNet.Shared.Contracts.Users;
using static Microsoft.AspNetCore.Http.StatusCodes;
using static System.Net.Mime.MediaTypeNames;

namespace BeselerNet.Api.Accounts;

internal static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder builder)
    {
        var v1 = builder.MapGroup("/v1/accounts")
            .WithTags("Accounts");

        v1.MapPost("/register-user", RegisterUserHandler.Handle)
            .WithName("RegisterUser")
            .WithDescription("Register a new user account.")
            .Accepts<RegisterUserRequest>(Application.Json)
            .Produces(Status201Created)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .AllowAnonymous();

        v1.MapPost("/oauth/tokens", CreateTokenHandler.Handle)
            .WithName("GetOAuthToken")
            .WithDescription("Get OAuth token")
            .Accepts<OAuthTokenRequest>(Application.Json)
            .Produces<OAuthTokenResponse>(Status200OK, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .AllowAnonymous();

        v1.MapPost("/resend-email-confirmation", ResendEmailVerificationHandler.Handle)
            .WithName("ResendEmailConfirmation")
            .WithDescription("Resend the email verification link.")
            .Produces(Status202Accepted)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status422UnprocessableEntity, Application.Json)
            .RequireAuthorization();

        v1.MapPost("/confirm-email", ConfirmEmailHandler.Handle)
            .WithName("ConfirmEmail")
            .WithDescription("Confirm the email address.")
            .Produces(Status204NoContent)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .RequireAuthorization();

        v1.MapPost("/forgot-password", ForgotPasswordHandler.Handle)
            .WithName("SendForgotPassword")
            .WithDescription("Send a password reset link to the email address.")
            .Accepts<ForgotPasswordRequest>(Application.Json)
            .Produces<GenericMessageResponse>(Status202Accepted)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .ProducesProblem(Status429TooManyRequests, Application.Json)
            .AllowAnonymous();

        v1.MapPost("/reset-password", ResetPasswordHandler.Handle)
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
