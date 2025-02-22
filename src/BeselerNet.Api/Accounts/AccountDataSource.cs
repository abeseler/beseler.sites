﻿using BeselerNet.Api.Core;
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
            _ = await connection.ExecuteAsync("""
            INSERT INTO account (
                account_id,
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
            SET username = @Username,
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

            List<OutboxMessage>? outboxMessages = null;
            foreach (var @event in account.UncommittedEvents)
            {
                var eventData = JsonSerializer.Serialize(@event, JsonSerializerOptions.Web);
                if (@event.SendToOutbox)
                {
                    outboxMessages ??= [];
                    outboxMessages.Add(new OutboxMessage
                    {
                        MessageId = @event.EventId,
                        MessageType = nameof(DomainEvent),
                        MessageData = eventData,
                        InvisibleUntil = @event.OccurredAt,
                        ReceivesRemaining = 3
                    });
                }
                _ = await connection.ExecuteAsync("""
                    INSERT INTO account_event_log (event_id,  account_id, event_data, occurred_at)
                    VALUES (@EventId, @AccountId, @EventData::jsonb, @OccurredAt)
                    """, new
                    {
                        @event.EventId,
                        account.AccountId,
                        EventData = eventData,
                        @event.OccurredAt
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
