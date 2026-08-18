using BeselerNet.Shared.Contracts.OAuth;

namespace BeselerNet.Web.Shared;

internal sealed record AuthTicket
{
    public required string Sid { get; init; }
    public required string AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTimeOffset AccessExpiresAt { get; init; }
    public bool Persist { get; init; }

    public bool AccessExpiresSoon(TimeProvider time) =>
        AccessExpiresAt <= time.GetUtcNow().AddSeconds(60);

    public static AuthTicket From(OAuthTokenResponse tokens, TimeProvider time, string? sid = null, bool? persist = null) => new()
    {
        Sid = sid ?? Guid.CreateVersion7().ToString("N"),
        AccessToken = tokens.AccessToken,
        RefreshToken = tokens.RefreshToken,
        AccessExpiresAt = time.GetUtcNow().AddSeconds(Math.Max(tokens.ExpiresIn, 0)),
        Persist = persist ?? false
    };
}
