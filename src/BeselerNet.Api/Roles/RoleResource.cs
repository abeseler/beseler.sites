using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Shared;

namespace BeselerNet.Api.Accounts;

internal sealed class RoleResource : IAuthorizableResource
{
    public static string ResourceName => Resources.Role;
}

internal sealed class PermissionResource : IAuthorizableResource
{
    public static string ResourceName => Resources.Permission;
}
