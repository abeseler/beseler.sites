using BeselerNet.Shared.Core;
using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Contracts.Users;

public sealed record InviteUserRequest
{
    public string Email { get; init; } = "";
    [JsonPropertyName("given_name")]
    public string GivenName { get; init; } = "";
    [JsonPropertyName("family_name")]
    public string FamilyName { get; init; } = "";
    public IReadOnlyList<AccountRoleAssignment> Roles { get; init; } = [];

    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();
        if (string.IsNullOrWhiteSpace(Email)) errors.Add("email", "Email is required.");
        else if (Email.Length >= 320) errors.Add("email", "Email is too long. It must be less than 320 characters.");
        else if (!Extensions.BasicEmailRegex().IsMatch(Email)) errors.Add("email", "Email is invalid.");

        if (string.IsNullOrWhiteSpace(GivenName)) errors.Add("given_name", "Given name is required.");
        else if (GivenName.Trim().Length > 64) errors.Add("given_name", "Given name must be 64 characters or fewer.");
        if (string.IsNullOrWhiteSpace(FamilyName)) errors.Add("family_name", "Family name is required.");
        else if (FamilyName.Trim().Length > 64) errors.Add("family_name", "Family name must be 64 characters or fewer.");

        var roles = new SetAccountRolesRequest { Roles = Roles };
        if (roles.IsInvalid(out var roleErrors) && roleErrors is not null)
        {
            foreach (var pair in roleErrors)
                foreach (var message in pair.Value)
                    errors.Add(pair.Key, message);
        }

        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}
