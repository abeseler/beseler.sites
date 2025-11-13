using BeselerNet.Shared.Core;
using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Contracts.OAuth;

public sealed record OAuthTokenRequest
{
    [JsonPropertyName("grant_type"), JsonConverter(typeof(JsonStringEnumConverter))]
    public OAuthGrantType GrantType { get; init; }
    [JsonPropertyName("client_id")]
    public string? ClientId { get; init; }
    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; init; }
    [JsonPropertyName("username")]
    public string? Username { get; init; }
    [JsonPropertyName("password")]
    public string? Password { get; init; }
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    public bool IsInvalid([NotNullWhen(true)] out Dictionary<string, string[]>? validationErrors)
    {
        var errors = new ErrorCollector();

        validationErrors = null;
        if (GrantType == OAuthGrantType.password)
        {
            if (string.IsNullOrWhiteSpace(Username)) errors.Add("username", "Username is required.");
            if (string.IsNullOrWhiteSpace(Password)) errors.Add("password", "Password is required.");
            if (string.IsNullOrWhiteSpace(ClientId)) errors.Add("client_id", "Client ID is required.");
        }
        else if (GrantType == OAuthGrantType.client_credentials)
        {
            if (string.IsNullOrWhiteSpace(ClientId)) errors.Add("client_id", "Client ID is required.");
            if (string.IsNullOrWhiteSpace(ClientSecret)) errors.Add("client_secret", "Client secret is required.");
        }
        else if (GrantType is not OAuthGrantType.refresh_token) errors.Add("grant_type", "Invalid grant type.");

        validationErrors = errors.Collection;
        return errors.Count > 0;
    }
}

public enum OAuthGrantType
{
    password,
    client_credentials,
    refresh_token
}
