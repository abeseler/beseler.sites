using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts;

internal static class AccountProblems
{
    public static ProblemDetails Locked { get; } = new()
    {
        Title = AuthLimits.AccountLockedTitle,
        Detail = "Your account is locked. Please contact support.",
        Status = StatusCodes.Status403Forbidden
    };
    public static ProblemDetails Disabled { get; } = new()
    {
        Title = "Account Disabled",
        Detail = "Your account is disabled. Please contact support.",
        Status = StatusCodes.Status403Forbidden
    };
    public static ProblemDetails EmailAlreadyVerified(string email) => new()
    {
        Title = "Email Already Verified",
        Detail = $"Your email address ({email}) has already been verified.",
        Status = StatusCodes.Status400BadRequest
    };

    public static ProblemDetails NotFound { get; } = new()
    {
        Title = "Account Not Found",
        Detail = "That account does not exist.",
        Status = StatusCodes.Status404NotFound
    };

    public static ProblemDetails CannotChangeSelf { get; } = new()
    {
        Title = "Cannot Change Own Account",
        Detail = "You cannot disable, enable, unlock, or change roles on your own account.",
        Status = StatusCodes.Status400BadRequest
    };

    public static ProblemDetails LastAdmin { get; } = new()
    {
        Title = "Last Admin",
        Detail = "At least one account must keep the admin role.",
        Status = StatusCodes.Status400BadRequest
    };

    public static ProblemDetails UnknownRoles { get; } = new()
    {
        Title = "Unknown Roles",
        Detail = "One or more role ids are not in the catalog.",
        Status = StatusCodes.Status400BadRequest
    };

    public static IResult? Forbid<TResource>(ClaimsPrincipal user, TResource resource, string action, string? requiredScope = null)
        where TResource : IAuthorizableResource
    {
        var auth = Authorizer.Authorize(user, resource, action, requiredScope);
        if (auth.Failed(out var exception))
        {
            return TypedResults.Problem(new()
            {
                Title = "Forbidden",
                Detail = exception.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }

        return null;
    }
}
