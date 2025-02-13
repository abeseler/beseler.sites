using Dapper;
using Npgsql;

namespace BeselerNet.Api.Accounts;

internal sealed class AccountDataSource(NpgsqlDataSource dataSource)
{
    public async Task<int> NextId(CancellationToken stoppingToken)
    {
        using var connection = await dataSource.OpenConnectionAsync(stoppingToken);
        return await connection.ExecuteScalarAsync<int>("SELECT nextval('accounts_id_seq')", stoppingToken);
    }

    public async Task<Account?> WithId(int id, CancellationToken stoppingToken)
    {
        using var connection = await dataSource.OpenConnectionAsync(stoppingToken);
        return await connection.QuerySingleOrDefaultAsync<Account>(
            "SELECT * FROM account WHERE account_id = @id", new { id });
    }

    public async Task<Account?> WithEmail(string email, CancellationToken stoppingToken)
    {
        using var connection = await dataSource.OpenConnectionAsync(stoppingToken);

        return await connection.QuerySingleOrDefaultAsync<Account>(
            "SELECT * FROM account WHERE email = @email", new { email });
    }

    public async Task SaveChanges(Account account, CancellationToken stoppingToken)
    {
        using var connection = await dataSource.OpenConnectionAsync(stoppingToken);
        await connection.ExecuteAsync("", account);
    }
}
