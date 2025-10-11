using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Contracts.Users;

public sealed partial record RegisterUserRequest
{
    public string Email { get; init; } = "";
    public string Password { get; init; } = "";
    [JsonPropertyName("given_name")]
    public string GivenName { get; init; } = "";
    [JsonPropertyName("family_name")]
    public string FamilyName { get; init; } = "";

    public bool HasValidationErrors([NotNullWhen(true)] out Dictionary<string, string[]>? errors)
    {
        errors = null;

        if (string.IsNullOrWhiteSpace(Email))
        {
            errors ??= [];
            errors["email"] = ["Email is required."];
        }
        else if (Email is not { Length: < 320 })
        {
            errors ??= [];
            errors["email"] = ["Email is too long. It must be less than 320 characters."];
        }
        else if (!Extensions.BasicEmailRegex().IsMatch(Email))
        {
            errors ??= [];
            errors["email"] = ["Email is invalid."];
        }
        if (string.IsNullOrWhiteSpace(Password))
        {
            errors ??= [];
            errors["password"] = ["Password is required."];
        }
        else if (Password is not { Length: > 7 })
        {
            errors ??= [];
            errors["password"] = ["Password is too short. It must be at least 8 characters."];
        }
        if (string.IsNullOrWhiteSpace(GivenName))
        {
            errors ??= [];
            errors["given_name"] = ["Given name is required."];
        }
        if (string.IsNullOrWhiteSpace(FamilyName))
        {
            errors ??= [];
            errors["family_name"] = ["Family name is required."];
        }

        return errors is not null;
    }
}
