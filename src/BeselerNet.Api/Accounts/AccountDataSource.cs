using BeselerNet.Api.Events;
using Dapper;
using Npgsql;
using System.Runtime.CompilerServices;

namespace BeselerNet.Api.Accounts;

internal sealed class AccountDataSource(NpgsqlDataSource dataSource, EventLogDataSource eventLog, ILogger<AccountDataSource> logger)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly EventLogDataSource _eventLog = eventLog;
    private readonly ILogger<AccountDataSource> _logger = logger;
    public async Task<int> NextId(CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        return await connection.ExecuteScalarAsync<int>("SELECT nextval('account_id_seq')");
    }

    public async Task<Account?> WithId(int id, CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        return await connection.QuerySingleOrDefaultAsync<Account>(
            "SELECT * FROM account WHERE account_id = @id", new { id });
    }

    public async Task<Account?> WithId_IncludePermissions(int id, CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        var results = await connection.QueryMultipleAsync(
            """
            SELECT * FROM account WHERE account_id = @id;

            SELECT ap.account_id, p.permission_id, p.resource, p.action, ap.scope, ap.granted_at, ap.granted_by_account_id
            FROM account_permission ap
            INNER JOIN permission p ON ap.permission_id = p.permission_id
            WHERE ap.account_id = @id;
            """, new { id });

        var account = await results.ReadSingleOrDefaultAsync<Account>();
        if (account is not null)
        {
            var permissions = await results.ReadAsync<AccountPermission>();
            PermissionsRef(account) = permissions.ToList();
        }
        return account;
    }

    public async Task<Account?> WithUsername(string username, CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        return await connection.QuerySingleOrDefaultAsync<Account>(
            "SELECT * FROM account WHERE username = @username", new { username });
    }

    public async Task<Account?> WithUsername_IncludePermissions(string username, CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        var results = await connection.QueryMultipleAsync(
            """
            SELECT * FROM account WHERE username = @username;

            SELECT ap.account_id, p.permission_id, p.resource, p.action, ap.scope, ap.granted_at, ap.granted_by_account_id
            FROM account_permission ap
            INNER JOIN account a ON ap.account_id = a.account_id
            INNER JOIN permission p ON ap.permission_id = p.permission_id
            WHERE a.username = @username;
            """, new { username });

        var account = await results.ReadSingleOrDefaultAsync<Account>();
        if (account is not null)
        {
            var permissions = await results.ReadAsync<AccountPermission>();
            PermissionsRef(account) = permissions.ToList();
        }
        return account;
    }

    public async Task<Account?> WithEmail(string email, CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        return await connection.QuerySingleOrDefaultAsync<Account>(
            "SELECT * FROM account WHERE email = @email", new { email });
    }

    public async Task SaveChanges(Account account, CancellationToken stoppingToken)
    {
        if (account.IsChanged is false)
        {
            return;
        }

        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        using var transaction = await connection.BeginTransactionAsync(stoppingToken);

        try
        {
            _ = await connection.ExecuteAsync("""
            INSERT INTO account (
                account_id,
                version,
                type,
                username,
                email,
                secret_hash,
                secret_hashed_at,
                given_name,
                family_name,
                created_at,
                disabled_at,
                locked_at,
                last_logon,
                failed_login_attempts)
            VALUES (
                @AccountId,
                @Version,
                @Type,
                @Username,
                @Email,
                @SecretHash,
                @SecretHashedAt,
                @GivenName,
                @FamilyName,
                @CreatedAt,
                @DisabledAt,
                @LockedAt,
                @LastLogon,
                @FailedLoginAttempts)
            ON CONFLICT (account_id) DO UPDATE
            SET version = @Version,
                username = @Username,
                email = @Email,
                secret_hash = @SecretHash,
                secret_hashed_at = @SecretHashedAt,
                given_name = @GivenName,
                family_name = @FamilyName,
                disabled_at = @DisabledAt,
                locked_at = @LockedAt,
                last_logon = @LastLogon,
                failed_login_attempts = @FailedLoginAttempts
            """, new
            {
                account.AccountId,
                account.Version,
                Type = account.Type.ToString(),
                account.Username,
                account.Email,
                account.SecretHash,
                account.SecretHashedAt,
                account.GivenName,
                account.FamilyName,
                account.CreatedAt,
                account.DisabledAt,
                account.LockedAt,
                account.LastLogon,
                account.FailedLoginAttempts
            }, transaction);

            foreach (var @event in account.UncommittedEvents)
            {
                if (@event is AccountPermissionGranted granted)
                {
                    await Upsert(granted);
                }
                else if (@event is AccountPermissionRevoked revoked)
                {
                    await Delete(revoked);
                }
                await _eventLog.Append(@event, connection, transaction, stoppingToken);
            }

            await transaction.CommitAsync(stoppingToken);

            _logger.LogDebug("Saved account {AccountId}.", account.AccountId);

            account.AcceptChanges();
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to save account changes.");

            await transaction.RollbackAsync(stoppingToken);
            throw;
        }
    }

    private async Task Upsert(AccountPermissionGranted granted)
    {
        using var connection = await _dataSource.OpenConnectionAsync();
        _ = await connection.ExecuteAsync("""
            INSERT INTO account_permission (
                account_id,
                permission_id,
                scope,
                granted_at,
                granted_by_account_id)
            VALUES (
                @AccountId,
                @PermissionId,
                @Scope,
                @GrantedAt,
                @GrantedByAccountId)
            ON CONFLICT (account_id, permission_id) DO UPDATE
            SET scope = @Scope,
                granted_at = @GrantedAt,
                granted_by_account_id = @GrantedByAccountId
            """, new
        {
            granted.AccountId,
            granted.PermissionId,
            granted.Scope,
            GrantedAt = granted.CreatedAt,
            granted.GrantedByAccountId
        });
    }

    private async Task Delete(AccountPermissionRevoked revoked)
    {
        using var connection = await _dataSource.OpenConnectionAsync();
        _ = await connection.ExecuteAsync("""
            DELETE FROM account_permission
            WHERE account_id = @AccountId
            AND permission_id = @PermissionId
            """, new
            {
                revoked.AccountId,
                revoked.PermissionId
            });
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_permissions")]
    private extern static ref List<AccountPermission> PermissionsRef(Account @this);
}
