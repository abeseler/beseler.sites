using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace BeselerNet.Shared.Contracts.Users;

public sealed partial record RegisterUserRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string GivenName { get; init; }
    public required string FamilyName { get; init; }

    public bool HasValidationErrors([NotNullWhen(true)] out Dictionary<string, string[]>? errors)
    {
        errors = null;

        if (string.IsNullOrWhiteSpace(Email))
        {
            errors ??= [];
            errors["Email"] = ["Email is required."];
        }
        else if (Email is not { Length: < 320 })
        {
            errors ??= [];
            errors["Email"] = ["Email is too long. It must be less than 320 characters."];
        }
        else if (!Extensions.BasicEmailRegex().IsMatch(Email))
        {
            errors ??= [];
            errors["Email"] = ["Email is invalid."];
        }
        if (string.IsNullOrWhiteSpace(Password))
        {
            errors ??= [];
            errors["Password"] = ["Password is required."];
        }
        else if (Password is not { Length: >7 })
        {
            errors ??= [];
            errors["Password"] = ["Password is too short. It must be at least 8 characters."];
        }
        if (string.IsNullOrWhiteSpace(GivenName))
        {
            errors ??= [];
            errors["GivenName"] = ["Given name is required."];
        }
        if (string.IsNullOrWhiteSpace(FamilyName))
        {
            errors ??= [];
            errors["FamilyName"] = ["Family name is required."];
        }

        return errors is not null;
    }
}
