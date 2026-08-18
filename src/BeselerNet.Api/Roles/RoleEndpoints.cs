using BeselerNet.Shared.Contracts.Roles;
using static Microsoft.AspNetCore.Http.StatusCodes;
using static System.Net.Mime.MediaTypeNames;

namespace BeselerNet.Api.Accounts;

internal static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder builder)
    {
        var roles = builder.MapGroup("/v1/roles")
            .WithTags("Roles")
            .RequireAuthorization();

        roles.MapGet("/", RoleHandlers.List)
            .WithName("ListRoles")
            .Produces<IReadOnlyList<RoleResponse>>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json);

        roles.MapGet("/{roleId:int}", RoleHandlers.Get)
            .WithName("GetRole")
            .Produces<RoleResponse>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);

        roles.MapPost("/", RoleHandlers.Create)
            .WithName("CreateRole")
            .Accepts<CreateRoleRequest>(Application.Json)
            .Produces<RoleResponse>(Status201Created, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json);

        roles.MapPut("/{roleId:int}", RoleHandlers.Update)
            .WithName("UpdateRole")
            .Accepts<UpdateRoleRequest>(Application.Json)
            .Produces<RoleResponse>(Status200OK, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);

        roles.MapDelete("/{roleId:int}", RoleHandlers.Delete)
            .WithName("DeleteRole")
            .Produces(Status204NoContent)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);

        roles.MapPut("/{roleId:int}/permissions", RoleHandlers.SetPermissions)
            .WithName("SetRolePermissions")
            .Accepts<SetRolePermissionsRequest>(Application.Json)
            .Produces<RoleResponse>(Status200OK, Application.Json)
            .ProducesValidationProblem(Status400BadRequest, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .ProducesProblem(Status404NotFound, Application.Json);

        builder.MapGet("/v1/permissions", RoleHandlers.ListPermissions)
            .WithName("ListPermissions")
            .WithTags("Roles")
            .Produces<IReadOnlyList<PermissionResponse>>(Status200OK, Application.Json)
            .Produces(Status401Unauthorized)
            .ProducesProblem(Status403Forbidden, Application.Json)
            .RequireAuthorization();
    }
}
