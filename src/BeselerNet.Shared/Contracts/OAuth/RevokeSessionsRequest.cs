namespace BeselerNet.Shared.Contracts.OAuth;

public sealed record RevokeSessionsRequest
{
    public bool Everywhere { get; init; }
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }
}
