using BeselerNet.Shared.Contracts.Roles;
using BeselerNet.Web.Shared;

namespace BeselerNet.Web.Features.Roles;

internal sealed class RoleService(ApiClient api)
{
    private readonly ApiClient _api = api;

    public Task<ApiResult<IReadOnlyList<RoleResponse>>> ListAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<IReadOnlyList<RoleResponse>>("/v1/roles", session: true, cancellationToken);

    public Task<ApiResult<IReadOnlyList<PermissionResponse>>> ListPermissionsAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<IReadOnlyList<PermissionResponse>>("/v1/permissions", session: true, cancellationToken);

    public Task<ApiResult<RoleResponse>> CreateAsync(string name, IReadOnlyList<int> permissionIds, CancellationToken cancellationToken = default) =>
        _api.PostAsync<RoleResponse>("/v1/roles", new CreateRoleRequest
        {
            Name = name,
            PermissionIds = permissionIds
        }, session: true, cancellationToken: cancellationToken);

    public Task<ApiResult<RoleResponse>> RenameAsync(int roleId, string name, CancellationToken cancellationToken = default) =>
        _api.PutAsync<RoleResponse>($"/v1/roles/{roleId}", new UpdateRoleRequest { Name = name }, session: true, cancellationToken);

    public Task<ApiResult> DeleteAsync(int roleId, CancellationToken cancellationToken = default) =>
        _api.DeleteAsync($"/v1/roles/{roleId}", session: true, cancellationToken);

    public Task<ApiResult<RoleResponse>> SetPermissionsAsync(int roleId, IReadOnlyList<int> permissionIds, CancellationToken cancellationToken = default) =>
        _api.PutAsync<RoleResponse>($"/v1/roles/{roleId}/permissions", new SetRolePermissionsRequest
        {
            PermissionIds = permissionIds
        }, session: true, cancellationToken);
}
