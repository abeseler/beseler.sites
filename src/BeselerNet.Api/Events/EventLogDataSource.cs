using BeselerNet.Api.Core;
using BeselerNet.Api.Outbox;
using Dapper;
using Npgsql;
using System.Data;
using System.Text.Json;

namespace BeselerNet.Api.Events;

internal sealed class EventLogDataSource(NpgsqlDataSource dataSource, OutboxDataSource outbox, ILogger<EventLogDataSource> logger)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly OutboxDataSource _outbox = outbox;
    private readonly ILogger<EventLogDataSource> _logger = logger;

    public async Task Append(DomainEvent @event, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = connection ?? await _dataSource.OpenConnectionAsync(cancellationToken);
        var tran = transaction ?? conn.BeginTransaction();

        try
        {
            var payload = JsonSerializer.Serialize(@event, JsonSerializerOptions.Web);
            _ = await conn.ExecuteAsync("""
                INSERT INTO event_log (event_id, aggregate_type, aggregate_id, version, payload, created_at)
                VALUES (@EventId, @AggregateType, @AggregateId, @Version, @payload::jsonb, @CreatedAt)
                """, new
            {
                @event.EventId,
                @event.AggregateType,
                @event.AggregateId,
                @event.Version,
                payload,
                @event.CreatedAt
            }, tran);

            if (@event.SendToOutbox)
            {
                var outboxMessage = new OutboxMessage
                {
                    MessageId = @event.EventId,
                    MessageType = nameof(DomainEvent),
                    MessageData = payload,
                    InvisibleUntil = @event.CreatedAt,
                    ReceivesRemaining = 3
                };
                await _outbox.Enqueue(outboxMessage, conn, tran, cancellationToken);
            }
        }
        finally
        {
            if (connection is null)
            {
                conn.Dispose();
            }
        }
    }
}
