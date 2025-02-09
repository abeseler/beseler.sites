using System.Diagnostics.CodeAnalysis;

namespace BeselerNet.Shared.Contracts;

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

    public bool IsValid([NotNullWhen(false)] out Dictionary<string, string[]>? errors)
    {
        errors = null;
        if (GrantType == OAuthGrantType.password)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                errors ??= [];
                errors["username"] = ["Username is required."];
            }
            if (string.IsNullOrWhiteSpace(Password))
            {
                errors ??= [];
                errors["password"] = ["Password is required."];
            }
        }
        else if (GrantType == OAuthGrantType.client_credentials)
        {
            if (string.IsNullOrWhiteSpace(ClientId))
            {
                errors ??= [];
                errors["client_id"] = ["Client ID is required."];
            }
            if (string.IsNullOrWhiteSpace(ClientSecret))
            {
                errors ??= [];
                errors["client_secret"] = ["Client secret is required."];
            }
        }
        else if (GrantType is not OAuthGrantType.refresh_token)
        {
            errors ??= [];
            errors["grant_type"] = ["Invalid grant type."];
        }
        return errors is null;
    }
}

public enum OAuthGrantType
{
    password,
    client_credentials,
    refresh_token
}
