using BeselerNet.Shared.Core;
using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Contracts.Roles;

public sealed record RoleResponse
{
    [JsonPropertyName("role_id")]
    public required int RoleId { get; init; }
    public required string Name { get; init; }
    public required bool Protected { get; init; }
    [JsonPropertyName("locked_grants")]
    public required bool LockedGrants { get; init; }
    [JsonPropertyName("user_count")]
    public int UserCount { get; init; }
    public IReadOnlyList<PermissionResponse> Permissions { get; init; } = [];
}

public sealed record PermissionResponse
{
    [JsonPropertyName("permission_id")]
    public required int PermissionId { get; init; }
    public required string Resource { get; init; }
    public required string Action { get; init; }
}

public sealed record CreateRoleRequest
{
    public string Name { get; init; } = "";
    [JsonPropertyName("permission_ids")]
    public IReadOnlyList<int> PermissionIds { get; init; } = [];

    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();
        if (string.IsNullOrWhiteSpace(Name)) errors.Add("name", "Name is required.");
        else if (Name.Trim().Length > 64) errors.Add("name", "Name must be 64 characters or fewer.");
        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}

public sealed record UpdateRoleRequest
{
    public string Name { get; init; } = "";

    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();
        if (string.IsNullOrWhiteSpace(Name)) errors.Add("name", "Name is required.");
        else if (Name.Trim().Length > 64) errors.Add("name", "Name must be 64 characters or fewer.");
        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}

public sealed record SetRolePermissionsRequest
{
    [JsonPropertyName("permission_ids")]
    public IReadOnlyList<int> PermissionIds { get; init; } = [];
}
