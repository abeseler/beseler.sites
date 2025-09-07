using Dapper;
using Microsoft.Extensions.Caching.Hybrid;
using Npgsql;

namespace BeselerNet.Api.Accounts.OAuth;

internal sealed record Permission
{
    public int PermissionId { get; init; }
    public required string Resource { get; init; }
    public required string Action { get; init; }
}

internal sealed class PermissionCollecton : Dictionary<string, Permission>
{
    public Permission? Get(string resource, string action)
    {
        Span<char> key = stackalloc char[resource.Length + action.Length + 1];
        resource.AsSpan().CopyTo(key);
        key[resource.Length] = ':';
        action.AsSpan().CopyTo(key[(resource.Length + 1)..]);
        _ = GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(key, out var permission);
        return permission;
    }
}

internal sealed class PermissionDataSource(NpgsqlDataSource dataSource, HybridCache cache)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly HybridCache _cache = cache;
    public async ValueTask<PermissionCollecton> GetCollection(CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync("Permissions", async token =>
        {
            var collection = new PermissionCollecton();
            foreach (var permission in await GetAll(token))
            {
                var key = $"{permission.Resource}:{permission.Action}";
                collection[key] = permission;
            }
            return collection;
        }, new()
        {
            LocalCacheExpiration = TimeSpan.FromHours(1),
            Expiration = TimeSpan.FromHours(4)
        }, cancellationToken: cancellationToken);
    }

    private async Task<IEnumerable<Permission>> GetAll(CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QueryAsync<Permission>("SELECT * FROM permission");
    }
}
