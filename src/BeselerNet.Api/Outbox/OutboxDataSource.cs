using BeselerNet.Api.Core;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data;

namespace BeselerNet.Api.Outbox;

internal sealed class OutboxDataSource(NpgsqlDataSource dataSource, IOptions<FeaturesOptions> features)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly FeaturesOptions _features = features.Value;

    public async Task SaveAll(IEnumerable<OutboxMessage> messages, IDbConnection? openConnection = null, IDbTransaction? transaction = null, CancellationToken stoppingToken = default)
    {
        if (!_features.OutboxEnabled)
        {
            return;
        }

        var connection = openConnection ?? await _dataSource.OpenConnectionAsync(stoppingToken);
        try
        {
            foreach (var message in messages)
            {
                await connection.ExecuteAsync(
                    """
                    INSERT INTO outbox (message_id, message_type, message_data, invisible_until, receives_remaining)
                    VALUES (@MessageId, @MessageType, @MessageData::json, @InvisibleUntil, @ReceivesRemaining)              
                    """, message, transaction);
            }
        }
        finally
        {
            if (openConnection is null)
            {
                connection.Dispose();
            }
        }
    }

    public async Task<OutboxMessage?> Dequeue(CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        return await connection.QuerySingleOrDefaultAsync<OutboxMessage>(
            """
            WITH messages AS (
                SELECT message_id
                FROM outbox
                WHERE receives_remaining > 0
                AND invisible_until <= NOW() AT TIME ZONE 'utc'
                LIMIT 1
                FOR UPDATE SKIP LOCKED
            )
            UPDATE outbox o
            SET invisible_until = NOW() AT TIME ZONE 'utc' + interval '60 seconds',
                receives_remaining = receives_remaining - 1
            FROM messages m
            WHERE m.message_id = o.message_id
            RETURNING *
            """);
    }

    public async Task Delete(Guid messageId, CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        await connection.ExecuteAsync(
            "DELETE FROM outbox WHERE message_id = @messageId", new { messageId });
    }
}
