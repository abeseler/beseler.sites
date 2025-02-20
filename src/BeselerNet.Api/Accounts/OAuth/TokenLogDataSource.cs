using Dapper;
using Npgsql;

namespace BeselerNet.Api.Accounts.OAuth;

internal sealed record TokenLog
{
    public Guid Jti { get; init; }
    public int AccountId { get; init; }
    public Guid? ReplacedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public void Revoke() => RevokedAt = DateTimeOffset.UtcNow;
    public void ReplaceWith(Guid jti) => ReplacedBy = jti;
    public static TokenLog Create(TokenResult result, int accountId) => new()
    {
        Jti = result.RefreshTokenId!.Value,
        AccountId = accountId,
        CreatedAt = DateTimeOffset.UtcNow,
        ExpiresAt = result.RefreshTokenExpires
    };
}

internal sealed class TokenLogDataSource(NpgsqlDataSource dataSource)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;
    public async Task<TokenLog?> WithJti(Guid jti, CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        return await connection.QuerySingleOrDefaultAsync<TokenLog>(
            "SELECT * FROM token_log WHERE jti = @jti", new { jti });
    }

    public async Task SaveChanges(TokenLog tokenLog, CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        await connection.ExecuteAsync("""
            INSERT INTO token_log (jti, account_id, replaced_by, created_at, expires_at, revoked_at)
            VALUES (@jti, @accountId, @replacedBy, @createdAt, @expiresAt, @revokedAt)
            ON CONFLICT (jti) DO UPDATE
            SET replaced_by = @replacedBy,
                created_at = @createdAt,
                expires_at = @expiresAt,
                revoked_at = @revokedAt
            """, tokenLog);
    }

    public async Task RevokeAll(int accountId, CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        await connection.ExecuteAsync("""
            UPDATE token_log
            SET revoked_at = now() AT TIME ZONE 'utc'
            WHERE account_id = @accountId
            AND revoked_at IS NULL
            """, new { accountId });
    }

    public async Task RevokeChain(Guid jti, CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        await connection.ExecuteAsync("""
            WITH RECURSIVE descendants AS (
                SELECT
                    jti,
                    replaced_by
                FROM token_log WHERE jti = @jti
                UNION ALL
                SELECT
                    tl.jti,
                    tl.replaced_by
                FROM token_log tl
                INNER JOIN descendants td
                    ON td.replaced_by = tl.jti
                WHERE tl.revoked_at IS NULL
            ),
            ancestors AS (
                SELECT
                    jti,
                    replaced_by
                FROM token_log WHERE replaced_by = @jti
                UNION ALL
                SELECT
                    tl.jti,
                    tl.replaced_by
                FROM token_log tl
                INNER JOIN ancestors ta
                    ON ta.jti = tl.replaced_by
                WHERE tl.revoked_at IS NULL
            ),
            ids AS (
                SELECT ta.jti FROM ancestors ta
                UNION
                SELECT td.jti FROM descendants td
            )
            UPDATE token_log tl
            SET revoked_at = now() AT TIME ZONE 'utc'
            FROM ids i
            WHERE i.jti = tl.jti
            """, new { jti });
    }
}
