using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Contracts.Users;

public sealed record ForgotPasswordRequest
{
    public string? Email { get; init; }
    [JsonIgnore]
    public string? TraceId { get; init; } = Activity.Current?.Id ?? Activity.Current?.ParentId;

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

        return errors is not null;
    }
}
