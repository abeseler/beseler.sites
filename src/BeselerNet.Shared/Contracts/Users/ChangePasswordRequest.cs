using BeselerNet.Shared;
using BeselerNet.Shared.Core;
using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Contracts.Users;

public sealed record ChangePasswordRequest
{
    [JsonPropertyName("current_password")]
    public string CurrentPassword { get; init; } = "";
    public string Password { get; init; } = "";

    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();

        if (string.IsNullOrWhiteSpace(CurrentPassword))
            errors.Add("current_password", "Current password is required.");

        if (string.IsNullOrWhiteSpace(Password))
            errors.Add("password", "Password is required.");
        else if (Password.Length < AuthLimits.PasswordMinLength)
            errors.Add("password", $"Password is too short. It must be at least {AuthLimits.PasswordMinLength} characters.");
        else if (Password == CurrentPassword)
            errors.Add("password", "New password must be different from the current password.");

        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}
