using BeselerNet.Api.Core;
using BeselerNet.Api.Outbox;
using Dapper;
using Npgsql;
using System.Runtime.CompilerServices;

namespace BeselerNet.Api.Accounts;

internal sealed class AccountDataSource(NpgsqlDataSource dataSource, OutboxDataSource outbox, ILogger<AccountDataSource> logger)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly OutboxDataSource _outbox = outbox;
    private readonly ILogger<AccountDataSource> _logger = logger;
    public async Task<int> NextId(CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>("SELECT nextval('account_id_seq')");
    }

    public async Task<IReadOnlyList<Account>> ListUsers(CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var results = await connection.QueryMultipleAsync(
            """
            SELECT * FROM account WHERE type = 'User' ORDER BY created_at;

            SELECT ar.account_id, ar.role_id, r.name, ar.scope, ar.granted_at, ar.granted_by_account_id
            FROM account_role ar
            INNER JOIN role r ON r.role_id = ar.role_id
            INNER JOIN account a ON a.account_id = ar.account_id
            WHERE a.type = 'User';
            """);

        var accounts = (await results.ReadAsync<Account>()).ToList();
        var roles = (await results.ReadAsync<AccountRole>()).ToLookup(role => role.AccountId);
        foreach (var account in accounts)
            RolesRef(account) = roles[account.AccountId].ToList();

        return accounts;
    }

    public async Task<Account?> WithId(int id, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Account>(
            "SELECT * FROM account WHERE account_id = @id", new { id });
    }

    public async Task<Account?> WithId_IncludeRoles(int id, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var results = await connection.QueryMultipleAsync(
            """
            SELECT * FROM account WHERE account_id = @id;

            SELECT ar.account_id, ar.role_id, r.name, ar.scope, ar.granted_at, ar.granted_by_account_id
            FROM account_role ar
            INNER JOIN role r ON r.role_id = ar.role_id
            WHERE ar.account_id = @id;
            """, new { id });

        var account = await results.ReadSingleOrDefaultAsync<Account>();
        if (account is not null)
        {
            var roles = await results.ReadAsync<AccountRole>();
            RolesRef(account) = roles.ToList();
        }
        return account;
    }

    public async Task<Account?> WithId_IncludePermissions(int id, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var results = await connection.QueryMultipleAsync(
            """
            SELECT * FROM account WHERE account_id = @id;

            SELECT ap.account_id, p.permission_id, p.resource, p.action, ap.scope, ap.granted_at, ap.granted_by_account_id
            FROM account_permission ap
            INNER JOIN permission p ON ap.permission_id = p.permission_id
            WHERE ap.account_id = @id;

            SELECT ar.account_id, p.permission_id, p.resource, p.action, ar.scope, ar.granted_at, ar.granted_by_account_id
            FROM account_role ar
            INNER JOIN role_permission rp ON rp.role_id = ar.role_id
            INNER JOIN permission p ON p.permission_id = rp.permission_id
            WHERE ar.account_id = @id;

            SELECT ar.account_id, ar.role_id, r.name, ar.scope, ar.granted_at, ar.granted_by_account_id
            FROM account_role ar
            INNER JOIN role r ON r.role_id = ar.role_id
            WHERE ar.account_id = @id;
            """, new { id });

        var account = await results.ReadSingleOrDefaultAsync<Account>();
        if (account is not null)
        {
            var permissions = await results.ReadAsync<AccountPermission>();
            var rolePermissions = await results.ReadAsync<AccountPermission>();
            var roles = await results.ReadAsync<AccountRole>();
            PermissionsRef(account) = permissions.ToList();
            RolePermissionsRef(account) = rolePermissions.ToList();
            RolesRef(account) = roles.ToList();
        }
        return account;
    }

    public async Task<Account?> WithUsername(string username, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Account>(
            "SELECT * FROM account WHERE username = @username", new { username });
    }

    public async Task<Account?> WithUsername_IncludePermissions(string username, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var results = await connection.QueryMultipleAsync(
            """
            SELECT * FROM account WHERE username = @username;

            SELECT ap.account_id, p.permission_id, p.resource, p.action, ap.scope, ap.granted_at, ap.granted_by_account_id
            FROM account_permission ap
            INNER JOIN account a ON ap.account_id = a.account_id
            INNER JOIN permission p ON ap.permission_id = p.permission_id
            WHERE a.username = @username;

            SELECT ar.account_id, p.permission_id, p.resource, p.action, ar.scope, ar.granted_at, ar.granted_by_account_id
            FROM account_role ar
            INNER JOIN account a ON ar.account_id = a.account_id
            INNER JOIN role_permission rp ON rp.role_id = ar.role_id
            INNER JOIN permission p ON p.permission_id = rp.permission_id
            WHERE a.username = @username;

            SELECT ar.account_id, ar.role_id, r.name, ar.scope, ar.granted_at, ar.granted_by_account_id
            FROM account_role ar
            INNER JOIN account a ON ar.account_id = a.account_id
            INNER JOIN role r ON r.role_id = ar.role_id
            WHERE a.username = @username;
            """, new { username });

        var account = await results.ReadSingleOrDefaultAsync<Account>();
        if (account is not null)
        {
            var permissions = await results.ReadAsync<AccountPermission>();
            var rolePermissions = await results.ReadAsync<AccountPermission>();
            var roles = await results.ReadAsync<AccountRole>();
            PermissionsRef(account) = permissions.ToList();
            RolePermissionsRef(account) = rolePermissions.ToList();
            RolesRef(account) = roles.ToList();
        }
        return account;
    }

    public async Task<Account?> WithEmail(string email, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Account>(
            "SELECT * FROM account WHERE email = @email", new { email });
    }

    public async Task SaveChanges(Account account, CancellationToken cancellationToken)
    {
        if (account.IsChanged is false)
        {
            return;
        }

        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            _ = await connection.ExecuteAsync("""
            INSERT INTO account (
                account_id,
                version,
                type,
                username,
                email,
                email_verified_at,
                secret_hash,
                secret_hashed_at,
                given_name,
                family_name,
                created_at,
                disabled_at,
                locked_at,
                last_logon,
                failed_login_attempts,
                invited_at)
            VALUES (
                @AccountId,
                @Version,
                @Type,
                @Username,
                @Email,
                @EmailVerifiedAt,
                @SecretHash,
                @SecretHashedAt,
                @GivenName,
                @FamilyName,
                @CreatedAt,
                @DisabledAt,
                @LockedAt,
                @LastLogon,
                @FailedLoginAttempts,
                @InvitedAt)
            ON CONFLICT (account_id) DO UPDATE
            SET version = @Version,
                username = @Username,
                email = @Email,
                email_verified_at = @EmailVerifiedAt,
                secret_hash = @SecretHash,
                secret_hashed_at = @SecretHashedAt,
                given_name = @GivenName,
                family_name = @FamilyName,
                disabled_at = @DisabledAt,
                locked_at = @LockedAt,
                last_logon = @LastLogon,
                failed_login_attempts = @FailedLoginAttempts,
                invited_at = @InvitedAt
            """, new
            {
                account.AccountId,
                account.Version,
                Type = account.Type.ToString(),
                account.Username,
                account.Email,
                account.EmailVerifiedAt,
                account.SecretHash,
                account.SecretHashedAt,
                account.GivenName,
                account.FamilyName,
                account.CreatedAt,
                account.DisabledAt,
                account.LockedAt,
                account.LastLogon,
                account.FailedLoginAttempts,
                account.InvitedAt
            }, transaction);

            await SyncPermissions(account, connection, transaction);
            await SyncRoles(account, connection, transaction);

            var notifyMessageQueued = false;
            foreach (var domainEvent in account.UncommittedEvents)
            {
                var domainEventMessage = DomainEventMessage.Wrap(domainEvent).ToOutboxMessage();
                await _outbox.Enqueue(domainEventMessage, connection, transaction, cancellationToken);
                notifyMessageQueued = true;
            }

            await transaction.CommitAsync(cancellationToken);
            
            _logger.LogDebug("Saved account {AccountId}.", account.AccountId);

            FinalizeChanges(account);
            if (notifyMessageQueued)
            {
                OutboxMonitor.NotifyMessageAvailable();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save account changes.");

            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteUser(int accountId, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await connection.ExecuteAsync("DELETE FROM token_log WHERE account_id = @accountId", new { accountId }, transaction);
        await connection.ExecuteAsync("DELETE FROM communication WHERE account_id = @accountId", new { accountId }, transaction);
        await connection.ExecuteAsync("DELETE FROM account WHERE account_id = @accountId AND type = 'User'", new { accountId }, transaction);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task SyncPermissions(Account account, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        await connection.ExecuteAsync(
            "DELETE FROM account_permission WHERE account_id = @AccountId",
            new { account.AccountId },
            transaction);

        foreach (var permission in account.Permissions)
        {
            await connection.ExecuteAsync("""
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
                """, permission, transaction);
        }
    }

    private static async Task SyncRoles(Account account, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        await connection.ExecuteAsync(
            "DELETE FROM account_role WHERE account_id = @AccountId",
            new { account.AccountId },
            transaction);

        foreach (var role in account.Roles)
        {
            await connection.ExecuteAsync("""
                INSERT INTO account_role (
                    account_id,
                    role_id,
                    scope,
                    granted_at,
                    granted_by_account_id)
                VALUES (
                    @AccountId,
                    @RoleId,
                    @Scope,
                    @GrantedAt,
                    @GrantedByAccountId)
                """, role, transaction);
        }
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_permissions")]
    private static extern ref List<AccountPermission> PermissionsRef(Account @this);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_rolePermissions")]
    private static extern ref List<AccountPermission> RolePermissionsRef(Account @this);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_roles")]
    private static extern ref List<AccountRole> RolesRef(Account @this);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "AcceptChanges")]
    private static extern void FinalizeChanges(Account @this);
}
