using BeselerNet.Api.Accounts;
using BeselerNet.Api.Core;
using BeselerNet.Api.Outbox;
using Dapper;
using Npgsql;
using System.Data;
using System.Text.Json;

namespace BeselerNet.Api.Events;

internal sealed class EventLogDataSource(NpgsqlDataSource dataSource, OutboxDataSource outbox)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly OutboxDataSource _outbox = outbox;

    public async Task Append(IEnumerable<DomainEvent> events, IDbConnection? openConnection = null, IDbTransaction? transaction = null, CancellationToken stoppingToken = default)
    {
        var connection = openConnection ?? await _dataSource.OpenConnectionAsync(stoppingToken);
        try
        {
            List<OutboxMessage>? outboxMessages = null;
            foreach (var @event in events)
            {
                var details = JsonSerializer.Serialize(@event, JsonSerializerOptions.Web);
                if (@event.SendToOutbox)
                {
                    outboxMessages ??= [];
                    outboxMessages.Add(new OutboxMessage
                    {
                        MessageId = @event.EventId,
                        MessageType = nameof(DomainEvent),
                        MessageData = details,
                        InvisibleUntil = @event.OccurredAt,
                        ReceivesRemaining = 3
                    });
                }
                _ = await connection.ExecuteAsync("""
                    INSERT INTO event_log (event_id, resource, resource_id, details, occurred_at)
                    VALUES (@EventId, @Resource, @ResourceId, @Details::jsonb, @OccurredAt)
                    """, new
                {
                    @event.EventId,
                    @event.Resource,
                    @event.ResourceId,
                    Details = details,
                    @event.OccurredAt
                }, transaction);
            }

            if (outboxMessages is { Count: > 0 })
            {
                await _outbox.SaveAll(outboxMessages, connection, transaction, stoppingToken);
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
}
