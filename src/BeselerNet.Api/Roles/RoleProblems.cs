using BeselerNet.Api.Accounts.OAuth;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts;

internal static class RoleProblems
{
    public static ProblemDetails NotFound { get; } = new()
    {
        Title = "Role Not Found",
        Detail = "That role does not exist.",
        Status = StatusCodes.Status404NotFound
    };

    public static ProblemDetails Protected { get; } = new()
    {
        Title = "Role Protected",
        Detail = "Built-in roles cannot be renamed or deleted.",
        Status = StatusCodes.Status403Forbidden
    };

    public static ProblemDetails LockedGrants { get; } = new()
    {
        Title = "Role Grants Locked",
        Detail = "This role's permissions cannot be changed.",
        Status = StatusCodes.Status403Forbidden
    };

    public static ProblemDetails NameTaken { get; } = new()
    {
        Title = "Role Name Taken",
        Detail = "A role with that name already exists.",
        Status = StatusCodes.Status400BadRequest
    };

    public static ProblemDetails UnknownPermissions { get; } = new()
    {
        Title = "Unknown Permissions",
        Detail = "One or more permission ids are not in the catalog.",
        Status = StatusCodes.Status400BadRequest
    };

    public static IResult? Forbid(ClaimsPrincipal user, string action)
    {
        var auth = Authorizer.Authorize(user, new RoleResource(), action, requiredScope: null);
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
