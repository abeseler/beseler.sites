using Dapper;
using Microsoft.Extensions.Caching.Hybrid;
using Npgsql;

namespace BeselerNet.Api.Settings;

internal sealed record AppSetting
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public int? UpdatedByAccountId { get; init; }
}

internal sealed class SettingDataSource(NpgsqlDataSource dataSource, HybridCache cache)
{
    private const string CacheKey = "AppSettings";
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly HybridCache _cache = cache;

    public async Task<IReadOnlyList<AppSetting>> List(CancellationToken cancellationToken)
    {
        var all = await GetAll(cancellationToken);
        return all.Values.OrderBy(setting => setting.Key).ToList();
    }

    public async Task<AppSetting?> Get(string key, CancellationToken cancellationToken)
    {
        var all = await GetAll(cancellationToken);
        return all.TryGetValue(key, out var setting) ? setting : null;
    }

    public async Task<bool> IsEnabled(string key, CancellationToken cancellationToken)
    {
        var setting = await Get(key, cancellationToken);
        return setting is not null && bool.TryParse(setting.Value, out var enabled) && enabled;
    }

    public async Task<AppSetting> Set(string key, string value, int? updatedByAccountId, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var setting = await connection.QuerySingleAsync<AppSetting>(
            """
            INSERT INTO app_setting (key, value, updated_at, updated_by_account_id)
            VALUES (@key, @value, NOW(), @updatedByAccountId)
            ON CONFLICT (key) DO UPDATE
            SET value = excluded.value,
                updated_at = excluded.updated_at,
                updated_by_account_id = excluded.updated_by_account_id
            RETURNING key, value, updated_at, updated_by_account_id
            """, new { key, value, updatedByAccountId });
        await _cache.RemoveAsync(CacheKey, cancellationToken);
        return setting;
    }

    private async Task<Dictionary<string, AppSetting>> GetAll(CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync(CacheKey, async token =>
        {
            using var connection = await _dataSource.OpenConnectionAsync(token);
            var rows = await connection.QueryAsync<AppSetting>("SELECT key, value, updated_at, updated_by_account_id FROM app_setting");
            return rows.ToDictionary(row => row.Key, StringComparer.Ordinal);
        }, new()
        {
            LocalCacheExpiration = TimeSpan.FromMinutes(5),
            Expiration = TimeSpan.FromHours(1)
        }, cancellationToken: cancellationToken);
    }
}
