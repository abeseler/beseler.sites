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

    public async Task Append(DomainEvent @event, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken stoppingToken = default)
    {
        var conn = connection ?? await _dataSource.OpenConnectionAsync(stoppingToken);
        var tran = transaction ?? conn.BeginTransaction();

        try
        {
            var details = JsonSerializer.Serialize(@event, JsonSerializerOptions.Web);
            _ = await conn.ExecuteAsync("""
                INSERT INTO event_log (event_id, resource_type, resource_id, details, occurred_at)
                VALUES (@EventId, @Resource, @ResourceId, @Details::jsonb, @OccurredAt)
                """, new
                {
                    @event.EventId,
                    @event.ResourceType,
                    @event.ResourceId,
                    Details = details,
                    @event.OccurredAt
                }, tran);

            if (@event.SendToOutbox)
            {
                var outboxMessage = new OutboxMessage
                {
                    MessageId = @event.EventId,
                    MessageType = nameof(DomainEvent),
                    MessageData = details,
                    InvisibleUntil = @event.OccurredAt,
                    ReceivesRemaining = 3
                };
                await _outbox.Enqueue(outboxMessage, conn, tran, stoppingToken);
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
