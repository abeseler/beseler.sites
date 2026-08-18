using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Shared;
using BeselerNet.Shared.Contracts.Roles;
using Npgsql;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts;

internal static class RoleHandlers
{
    public static async Task<IResult> List(ClaimsPrincipal user, RoleDataSource roles, CancellationToken cancellationToken)
    {
        if (RoleProblems.Forbid(user, Actions.Read) is { } denied)
            return denied;

        return TypedResults.Ok(await roles.List(cancellationToken));
    }

    public static async Task<IResult> Get(int roleId, ClaimsPrincipal user, RoleDataSource roles, CancellationToken cancellationToken)
    {
        if (RoleProblems.Forbid(user, Actions.Read) is { } denied)
            return denied;

        var role = await roles.WithId(roleId, cancellationToken);
        return role is null ? TypedResults.Problem(RoleProblems.NotFound) : TypedResults.Ok(role);
    }

    public static async Task<IResult> Create(CreateRoleRequest request, ClaimsPrincipal user, RoleDataSource roles, CancellationToken cancellationToken)
    {
        if (RoleProblems.Forbid(user, Actions.Update) is { } denied)
            return denied;

        if (request.IsInvalid(out var errors))
            return TypedResults.ValidationProblem(errors);

        if (!await roles.PermissionsExist(request.PermissionIds, cancellationToken))
            return TypedResults.Problem(RoleProblems.UnknownPermissions);

        try
        {
            var role = await roles.Create(request.Name.Trim(), request.PermissionIds, cancellationToken);
            return TypedResults.Created($"/v1/roles/{role.RoleId}", role);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return TypedResults.Problem(RoleProblems.NameTaken);
        }
    }

    public static async Task<IResult> Update(int roleId, UpdateRoleRequest request, ClaimsPrincipal user, RoleDataSource roles, CancellationToken cancellationToken)
    {
        if (RoleProblems.Forbid(user, Actions.Update) is { } denied)
            return denied;

        if (request.IsInvalid(out var errors))
            return TypedResults.ValidationProblem(errors);

        var existing = await roles.Details(roleId, cancellationToken);
        if (existing is null)
            return TypedResults.Problem(RoleProblems.NotFound);
        if (existing.Protected)
            return TypedResults.Problem(RoleProblems.Protected);

        try
        {
            await roles.Rename(roleId, request.Name.Trim(), cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return TypedResults.Problem(RoleProblems.NameTaken);
        }

        return TypedResults.Ok(await roles.WithId(roleId, cancellationToken));
    }

    public static async Task<IResult> Delete(int roleId, ClaimsPrincipal user, RoleDataSource roles, CancellationToken cancellationToken)
    {
        if (RoleProblems.Forbid(user, Actions.Update) is { } denied)
            return denied;

        var existing = await roles.Details(roleId, cancellationToken);
        if (existing is null)
            return TypedResults.Problem(RoleProblems.NotFound);
        if (existing.Protected)
            return TypedResults.Problem(RoleProblems.Protected);

        await roles.Delete(roleId, cancellationToken);
        return TypedResults.NoContent();
    }

    public static async Task<IResult> SetPermissions(int roleId, SetRolePermissionsRequest request, ClaimsPrincipal user, RoleDataSource roles, CancellationToken cancellationToken)
    {
        if (RoleProblems.Forbid(user, Actions.Update) is { } denied)
            return denied;

        var existing = await roles.Details(roleId, cancellationToken);
        if (existing is null)
            return TypedResults.Problem(RoleProblems.NotFound);
        if (existing.LockedGrants)
            return TypedResults.Problem(RoleProblems.LockedGrants);

        if (!await roles.PermissionsExist(request.PermissionIds, cancellationToken))
            return TypedResults.Problem(RoleProblems.UnknownPermissions);

        await roles.SetPermissions(roleId, request.PermissionIds, cancellationToken);
        return TypedResults.Ok(await roles.WithId(roleId, cancellationToken));
    }

    public static async Task<IResult> ListPermissions(ClaimsPrincipal user, PermissionDataSource permissions, CancellationToken cancellationToken)
    {
        var auth = Authorizer.Authorize(user, new PermissionResource(), Actions.Read, requiredScope: null);
        if (auth.Failed(out var exception))
        {
            return TypedResults.Problem(new()
            {
                Title = "Forbidden",
                Detail = exception.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }

        var catalog = await permissions.List(cancellationToken);
        return TypedResults.Ok(catalog.Select(permission => new PermissionResponse
        {
            PermissionId = permission.PermissionId,
            Resource = permission.Resource,
            Action = permission.Action
        }));
    }
}
