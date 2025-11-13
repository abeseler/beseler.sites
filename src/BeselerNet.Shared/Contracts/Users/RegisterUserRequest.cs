using BeselerNet.Shared.Core;
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

    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();

        if (string.IsNullOrWhiteSpace(Email)) errors.Add("email", "Email is required.");
        else if (Email.Length >= 320) errors.Add("email", "Email is too long. It must be less than 320 characters.");
        else if (!Extensions.BasicEmailRegex().IsMatch(Email)) errors.Add("email", "Email is invalid.");

        if (string.IsNullOrWhiteSpace(Password)) errors.Add("password", "Password is required.");
        else if (Password.Length <= 7) errors.Add("password", "Password is too short. It must be at least 8 characters.");

        if (string.IsNullOrWhiteSpace(GivenName)) errors.Add("given_name", "Given name is required.");
        if (string.IsNullOrWhiteSpace(FamilyName)) errors.Add("family_name", "Family name is required.");

        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}
