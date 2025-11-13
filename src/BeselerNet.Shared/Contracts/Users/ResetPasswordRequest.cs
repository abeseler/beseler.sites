using BeselerNet.Shared.Core;
using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Contracts.Users;

public sealed record ResetPasswordRequest
{
    public string? Password { get; init; }
    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();
        
        if (string.IsNullOrWhiteSpace(Password)) errors.Add("password", "Password is required.");

        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}
