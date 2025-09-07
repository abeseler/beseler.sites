using Dapper;
using Npgsql;
using System.Data;

namespace BeselerNet.Api.Outbox;

internal sealed class OutboxDataSource(NpgsqlDataSource dataSource)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;

    public async Task Enqueue(OutboxMessage message, IDbConnection? openConnection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var connection = openConnection ?? await _dataSource.OpenConnectionAsync(cancellationToken);

        try
        {
            _ = await connection.ExecuteAsync(
                """
                INSERT INTO outbox (message_id, message_type, message_data, invisible_until, receives_remaining)
                VALUES (@MessageId, @MessageType, @MessageData::json, @InvisibleUntil, @ReceivesRemaining)              
                """, message, transaction);
        }
        finally
        {
            if (openConnection is null)
            {
                connection.Dispose();
            }
        }
    }

    public async Task<OutboxMessage[]> Dequeue(int dequeueMessageLimit, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var messages = await connection.QueryAsync<OutboxMessage>(
            """
            WITH messages AS (
                SELECT message_id
                FROM outbox
                WHERE receives_remaining > 0
                AND invisible_until <= NOW() AT TIME ZONE 'utc'
                LIMIT @dequeueMessageLimit
                FOR UPDATE SKIP LOCKED
            )
            UPDATE outbox o
            SET invisible_until = NOW() AT TIME ZONE 'utc' + interval '60 seconds',
                receives_remaining = receives_remaining - 1
            FROM messages m
            WHERE m.message_id = o.message_id
            RETURNING *
            """, new { dequeueMessageLimit });
        return [.. messages];
    }

    public async Task<int> Delete(Guid messageId, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteAsync("DELETE FROM outbox WHERE message_id = @messageId", new { messageId });
    }
}
