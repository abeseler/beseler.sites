using BeselerNet.Shared.Core;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Contracts.Users;

public sealed record ForgotPasswordRequest
{
    public string? Email { get; init; }
    [JsonIgnore]
    public string? TraceId { get; init; } = Activity.Current?.Id ?? Activity.Current?.ParentId;

    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();

        if (string.IsNullOrWhiteSpace(Email)) errors.Add("email", "Email is required.");
        if (Email is { Length: >= 320 }) errors.Add("email", "Email is too long. It must be less than 320 characters.");
        if (Email is { } && !Extensions.BasicEmailRegex().IsMatch(Email)) errors.Add("email", "Email is invalid.");

        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}
