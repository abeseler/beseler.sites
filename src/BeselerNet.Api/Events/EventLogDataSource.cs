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

    public async Task<int> Append(IEnumerable<DomainEvent> events, IDbConnection? openConnection = null, IDbTransaction? transaction = null, CancellationToken stoppingToken = default)
    {
        var connection = openConnection ?? await _dataSource.OpenConnectionAsync(stoppingToken);
        var count = 0;

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
                count += await connection.ExecuteAsync("""
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

            if (outboxMessages is { Count: >0 })
            {
                _ = await _outbox.Enqueue(outboxMessages, connection, transaction, stoppingToken);
            }
        }
        finally
        {
            if (openConnection is null)
            {
                connection.Dispose();
            }
        }

        _logger.LogDebug("Events appended to event log. Count: {Count}", count);

        return count;
    }
}
