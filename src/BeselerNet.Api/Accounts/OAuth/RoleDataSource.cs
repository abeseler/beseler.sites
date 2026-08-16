using Dapper;
using Microsoft.Extensions.Caching.Hybrid;
using Npgsql;

namespace BeselerNet.Api.Accounts.OAuth;

internal sealed class RoleDataSource(NpgsqlDataSource dataSource, HybridCache cache)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly HybridCache _cache = cache;

    public async ValueTask<Role?> WithName(string name, CancellationToken cancellationToken)
    {
        var roles = await GetCollection(cancellationToken);
        return roles.TryGetValue(name, out var role) ? role : null;
    }

    public async ValueTask<Dictionary<string, Role>> GetCollection(CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync("Roles", async token =>
        {
            using var connection = await _dataSource.OpenConnectionAsync(token);
            var roles = await connection.QueryAsync<Role>("SELECT role_id, name FROM role");
            return roles.ToDictionary(r => r.Name, StringComparer.Ordinal);
        }, new()
        {
            LocalCacheExpiration = TimeSpan.FromHours(1),
            Expiration = TimeSpan.FromHours(4)
        }, cancellationToken: cancellationToken);
    }
}
