using Dapper;
using Npgsql;

namespace BeselerNet.Api.Accounts;

internal sealed class AccountDataSource(NpgsqlDataSource dataSource)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;
    public async Task<int> NextId(CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        return await connection.ExecuteScalarAsync<int>("SELECT nextval('accounts_id_seq')");
    }

    public async Task<Account?> WithId(int id, CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        return await connection.QuerySingleOrDefaultAsync<Account>(
            "SELECT * FROM account WHERE account_id = @id", new { id });
    }

    public async Task<Account?> WithUsername(string username, CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        return await connection.QuerySingleOrDefaultAsync<Account>(
            "SELECT * FROM account WHERE username = @username", new { username });
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
        await connection.ExecuteAsync("""
            INSERT INTO account (
                account_id,
                username,
                email,
                secret_hash,
                secret_hashed_on,
                given_name,
                family_name,
                created_on,
                disabled_on,
                locked_on,
                last_logon,
                failed_login_attempts)
            VALUES (
                @AccountId,
                @Username,
                @Email,
                @SecretHash,
                @SecretHashedOn,
                @GivenName,
                @FamilyName,
                @CreatedOn,
                @DisabledOn,
                @LockedOn,
                @LastLogon,
                @FailedLoginAttempts)
            ON CONFLICT (account_id) DO UPDATE
            SET username = @Username,
                email = @Email,
                secret_hash = @SecretHash,
                secret_hashed_on = @SecretHashedOn,
                given_name = @GivenName,
                family_name = @FamilyName,
                created_on = @CreatedOn,
                disabled_on = @DisabledOn,
                locked_on = @LockedOn,
                last_logon = @LastLogon,
                failed_login_attempts = @FailedLoginAttempts
            """, account);
        account.AcceptChanges();
    }
}
