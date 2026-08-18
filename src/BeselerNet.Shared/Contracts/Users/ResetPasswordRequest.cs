using BeselerNet.Shared.Core;
using BeselerNet.Shared;
using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Contracts.Users;

public sealed record ResetPasswordRequest
{
    public string? Password { get; init; }
    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();
        
        if (string.IsNullOrWhiteSpace(Password)) errors.Add("password", "Password is required.");
        else if (Password.Length < AuthLimits.PasswordMinLength) errors.Add("password", $"Password is too short. It must be at least {AuthLimits.PasswordMinLength} characters.");

        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}
