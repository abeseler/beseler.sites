using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Contracts.Users;

public sealed record ResetPasswordRequest
{
    public string? Password { get; init; }
    public bool HasValidationErrors([NotNullWhen(true)] out Dictionary<string, string[]>? errors)
    {
        errors = null;

        if (string.IsNullOrWhiteSpace(Password))
        {
            errors ??= [];
            errors["Password"] = ["Password is required."];
        }

        return errors is not null;
    }
}
