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

        v1.MapGet("/me", GetCurrentAccountHandler.Handle)
            .WithName("GetCurrentAccount")
            .WithDescription("Get the signed-in account profile, including role membership.")
            .Produces<AccountProfileResponse>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .RequireAuthorization();

        v1.MapGet("/", AccountHandlers.List)
            .WithName("ListAccounts")
            .WithDescription("List user accounts. Requires account:read at a scope that applies without a specific account (typically global).")
            .Produces<IReadOnlyList<AccountResponse>>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .RequireAuthorization();

        v1.MapGet("/{accountId:int}", AccountHandlers.Get)
            .WithName("GetAccount")
            .WithDescription("Get an account. The owner can read their own; global can read any.")
            .Produces<AccountResponse>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json)
            .RequireAuthorization();

        v1.MapPut("/{accountId:int}", AccountHandlers.Update)
            .WithName("UpdateAccount")
            .WithDescription("Update given and family name. The owner can update their own; global can update any.")
            .Accepts<UpdateAccountRequest>(Application.Json)
            .Produces<AccountResponse>(Status200OK, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json)
            .RequireAuthorization();

        v1.MapPost("/{accountId:int}/disable", AccountHandlers.Disable)
            .WithName("DisableAccount")
            .WithDescription("Disable an account. Requires account:update at global. Cannot target yourself.")
            .Produces<AccountResponse>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json)
            .RequireAuthorization();

        v1.MapPost("/{accountId:int}/enable", AccountHandlers.Enable)
            .WithName("EnableAccount")
            .WithDescription("Re-enable an account. Requires account:update at global. Cannot target yourself.")
            .Produces<AccountResponse>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json)
            .RequireAuthorization();

        v1.MapPost("/{accountId:int}/unlock", AccountHandlers.Unlock)
            .WithName("UnlockAccount")
            .WithDescription("Clear lockout. Requires account:update at global. Cannot target yourself.")
            .Produces<AccountResponse>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json)
            .RequireAuthorization();

        v1.MapPost("/invite", InviteUserHandler.Handle)
            .WithName("InviteUser")
            .WithDescription("Create an invited account and email a set-password link. Requires account:update at global.")
            .Accepts<InviteUserRequest>(Application.Json)
            .Produces<AccountResponse>(Status201Created, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .RequireAuthorization();

        v1.MapPost("/{accountId:int}/resend-invite", InviteUserHandler.Resend)
            .WithName("ResendInvite")
            .WithDescription("Resend the invite email. Requires account:update at global.")
            .Produces(Status202Accepted)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json)
            .RequireAuthorization();

        v1.MapPost("/accept-invite", AcceptInviteHandler.Handle)
            .WithName("AcceptInvite")
            .WithDescription("Set a password from an invite link and return a session.")
            .Accepts<ResetPasswordRequest>(Application.Json)
            .Produces<OAuthTokenResponse>(Status200OK, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .RequireAuthorization();

        v1.MapPut("/{accountId:int}/roles", AccountHandlers.SetRoles)
            .WithName("SetAccountRoles")
            .WithDescription("Replace the account's roles. Requires account:update at global. Cannot target yourself or remove the last admin.")
            .Accepts<SetAccountRolesRequest>(Application.Json)
            .Produces<AccountResponse>(Status200OK, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json)
            .RequireAuthorization();

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
            .WithDescription("Confirm the email address and return a fresh session.")
            .Produces<OAuthTokenResponse>(Status200OK, Application.Json)
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
            .WithDescription("Reset the password and return a fresh session.")
            .Accepts<ResetPasswordRequest>(Application.Json)
            .Produces<OAuthTokenResponse>(Status200OK, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .RequireAuthorization();
    }
}
