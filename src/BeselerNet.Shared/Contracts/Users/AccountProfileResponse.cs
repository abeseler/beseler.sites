namespace BeselerNet.Shared.Contracts.Users;

public sealed record AccountProfileResponse
{
    [JsonPropertyName("account_id")]
    public required int AccountId { get; init; }
    public required string Username { get; init; }
    public string? Email { get; init; }
    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; init; }
    [JsonPropertyName("given_name")]
    public string? GivenName { get; init; }
    [JsonPropertyName("family_name")]
    public string? FamilyName { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<AccountRoleResponse> Roles { get; init; } = [];
    public IReadOnlyList<AccountPermissionResponse> Permissions { get; init; } = [];

    public bool HasRole(string name) =>
        Roles.Any(role => string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase));

    public bool HasPermission(string resource, string action, string? scope = null)
    {
        var permission = Permissions.FirstOrDefault(p =>
            string.Equals(p.Resource, resource, StringComparison.OrdinalIgnoreCase)
            && string.Equals(p.Action, action, StringComparison.OrdinalIgnoreCase));

        if (permission is null)
            return false;

        return scope is null || permission.Scopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record AccountRoleResponse
{
    public required string Name { get; init; }
    public required string Scope { get; init; }
}

public sealed record AccountPermissionResponse
{
    public required string Resource { get; init; }
    public required string Action { get; init; }
    public IReadOnlyList<string> Scopes { get; init; } = [];
}
