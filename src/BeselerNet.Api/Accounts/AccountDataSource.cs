using BeselerNet.Api.Core;
using BeselerNet.Api.Outbox;
using Dapper;
using Npgsql;
using System.Text.Json;

namespace BeselerNet.Api.Accounts;

internal sealed class AccountDataSource(NpgsqlDataSource dataSource, OutboxDataSource outbox)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly OutboxDataSource _outbox = outbox;
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

    public async Task<IEnumerable<DomainEvent>> DomainEvents(int accountId, CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        return await connection.QueryAsync<DomainEvent>(
            "SELECT event_data FROM account_event_log WHERE account_id = @accountId", new { accountId });
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
            await connection.ExecuteAsync("""
            INSERT INTO account (
                account_id,
                type,
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
                failed_login_attempts,
                event_log_count)
            VALUES (
                @AccountId,
                @Type,
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
                @FailedLoginAttempts,
                @EventLogCount)
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
                failed_login_attempts = @FailedLoginAttempts,
                event_log_count = @EventLogCount
            """, new
            {
                account.AccountId,
                Type = account.Type.ToString(),
                account.Username,
                account.Email,
                account.SecretHash,
                account.SecretHashedOn,
                account.GivenName,
                account.FamilyName,
                account.CreatedOn,
                account.DisabledOn,
                account.LockedOn,
                account.LastLogon,
                account.FailedLoginAttempts,
                account.EventLogCount
            }, transaction);

            List<OutboxMessage>? outboxMessages = null;
            foreach (var @event in account.UncommittedEvents)
            {
                var eventData = JsonSerializer.Serialize(@event, JsonSerializerOptions.Web);
                if (@event.PublishToOutbox)
                {
                    outboxMessages ??= [];
                    outboxMessages.Add(new OutboxMessage
                    {
                        MessageId = @event.EventId,
                        MessageType = nameof(DomainEvent),
                        MessageData = eventData,
                        InvisibleUntil = @event.OccurredOn,
                        ReceivesRemaining = 3
                    });
                }
                await connection.ExecuteAsync("""
                INSERT INTO account_event_log (event_id,  account_id, event_data, occurred_on)
                VALUES (@EventId, @AccountId, @EventData::jsonb, @OccurredOn)
                """, new
                {
                    @event.EventId,
                    account.AccountId,
                    EventData = eventData,
                    @event.OccurredOn
                }, transaction);
            }

            if (outboxMessages is { Count: >0 })
            {
                await _outbox.SaveAll(outboxMessages, connection, transaction, stoppingToken);
            }

            await transaction.CommitAsync(stoppingToken);

            account.AcceptChanges();
        }
        catch
        {
            await transaction.RollbackAsync(stoppingToken);
            throw;
        }
    }
}
