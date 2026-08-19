using System.Security.Cryptography;
using System.Text;

namespace BeselerNet.Shared;

public sealed class OAuthOptions
{
    public const string SectionName = "OAuth";

    public string WebClientId { get; init; } = "beseler-net-web";
    public string WebClientSecret { get; init; } = "";

    public bool IsWebClient(string? clientId, string? clientSecret)
    {
        if (string.IsNullOrEmpty(WebClientId) || string.IsNullOrEmpty(WebClientSecret)
            || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return false;
        }

        var idMatch = FixedEquals(clientId, WebClientId);
        var secretMatch = FixedEquals(clientSecret, WebClientSecret);
        return idMatch && secretMatch;
    }

    private static bool FixedEquals(string left, string right)
    {
        var leftHash = SHA256.HashData(Encoding.UTF8.GetBytes(left));
        var rightHash = SHA256.HashData(Encoding.UTF8.GetBytes(right));
        return CryptographicOperations.FixedTimeEquals(leftHash, rightHash);
    }
}
