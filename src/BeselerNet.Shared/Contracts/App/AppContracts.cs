using BeselerNet.Shared.Core;
using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Contracts.App;

public sealed record AppStatusResponse
{
    [JsonPropertyName("signup_open")]
    public required bool SignupOpen { get; init; }
    [JsonPropertyName("api_version")]
    public string? ApiVersion { get; init; }
}

public sealed record AppSettingResponse
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; }
    [JsonPropertyName("updated_by_account_id")]
    public int? UpdatedByAccountId { get; init; }
}

public sealed record UpdateSettingRequest
{
    public string Value { get; init; } = "";

    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();
        if (string.IsNullOrWhiteSpace(Value))
            errors.Add("value", "Value is required.");
        else if (!bool.TryParse(Value, out _))
            errors.Add("value", "Value must be true or false.");
        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}
