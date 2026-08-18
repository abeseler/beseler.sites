using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace BeselerNet.Web.Shared;

internal sealed class AccessClaims
{
    public static AccessClaims Empty { get; } = new(new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase));

    private readonly Dictionary<string, string[]> _grants;

    private AccessClaims(Dictionary<string, string[]> grants) => _grants = grants;

    public bool Has(string resource, string action, string? scope = null)
    {
        if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(action))
            return false;

        if (!_grants.TryGetValue($"{resource}:{action}", out var scopes))
            return false;

        return scope is null || scopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
    }

    public static AccessClaims FromAccessToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Empty;

        var dot = token.IndexOf('.');
        if (dot < 0)
            return Empty;

        var end = token.IndexOf('.', dot + 1);
        if (end < 0)
            return Empty;

        try
        {
            var json = WebEncoders.Base64UrlDecode(token[(dot + 1)..end]);
            using var document = JsonDocument.Parse(json);
            var grants = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!property.Name.Contains(':'))
                    continue;

                var scopes = ReadScopes(property.Value);
                if (scopes.Length > 0)
                    grants[property.Name] = scopes;
            }

            return grants.Count == 0 ? Empty : new AccessClaims(grants);
        }
        catch (Exception)
        {
            return Empty;
        }
    }

    private static string[] ReadScopes(JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.String)
        {
            return value.GetString()?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? [];
        }

        if (value.ValueKind is not JsonValueKind.Array)
            return [];

        return [.. value.EnumerateArray()
            .Select(item => item.GetString())
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope!)];
    }
}
