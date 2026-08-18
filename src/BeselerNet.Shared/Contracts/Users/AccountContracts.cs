using BeselerNet.Shared.Core;
using System.Diagnostics.CodeAnalysis;
using BeselerNet.Shared;

namespace BeselerNet.Shared.Contracts.Users;

public sealed record AccountResponse
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
    public required string Type { get; init; }
    public bool Disabled { get; init; }
    public bool Locked { get; init; }
    [JsonPropertyName("last_logon")]
    public DateTimeOffset? LastLogon { get; init; }
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyList<AccountRoleResponse> Roles { get; init; } = [];
}

public sealed record UpdateAccountRequest
{
    [JsonPropertyName("given_name")]
    public string GivenName { get; init; } = "";
    [JsonPropertyName("family_name")]
    public string FamilyName { get; init; } = "";

    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();
        if (string.IsNullOrWhiteSpace(GivenName)) errors.Add("given_name", "Given name is required.");
        else if (GivenName.Trim().Length > 64) errors.Add("given_name", "Given name must be 64 characters or fewer.");
        if (string.IsNullOrWhiteSpace(FamilyName)) errors.Add("family_name", "Family name is required.");
        else if (FamilyName.Trim().Length > 64) errors.Add("family_name", "Family name must be 64 characters or fewer.");
        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}

public sealed record SetAccountRolesRequest
{
    public IReadOnlyList<AccountRoleAssignment> Roles { get; init; } = [];

    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();
        var seen = new HashSet<int>();
        for (var i = 0; i < Roles.Count; i++)
        {
            var assignment = Roles[i];
            if (assignment.RoleId <= 0)
                errors.Add("roles", i, "role_id", "Role is required.");
            else if (!seen.Add(assignment.RoleId))
                errors.Add("roles", i, "role_id", "Each role can only be assigned once.");

            if (string.IsNullOrWhiteSpace(assignment.Scope))
                errors.Add("roles", i, "scope", "Scope is required.");
            else if (!ValidScopes.Contains(assignment.Scope))
                errors.Add("roles", i, "scope", "Scope must be owned, shared, global, or self.");
        }

        validationErrors = errors.Collection;
        return errors.Count > 0;
    }

    private static readonly HashSet<string> ValidScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        Scopes.Owned,
        Scopes.Shared,
        Scopes.Global,
        Scopes.Self
    };
}

public sealed record AccountRoleAssignment
{
    [JsonPropertyName("role_id")]
    public int RoleId { get; init; }
    public string Scope { get; init; } = "";
}
