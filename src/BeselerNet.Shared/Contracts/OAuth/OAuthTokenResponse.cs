namespace BeselerNet.Shared.Contracts.OAuth;

public sealed record class OAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "Bearer";
    [JsonPropertyName("expires_in")]
    public long ExpiresIn { get; init; }
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }
}
