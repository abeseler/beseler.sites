using Dapper;
using Npgsql;

namespace BeselerNet.Api.Events;

internal sealed class EventLogDataSource(NpgsqlDataSource dataSource)
{
    public async Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM event_log WHERE event_id = @eventId)", new { eventId });
    }

    public async Task Append(EventLogEntity entry, CancellationToken cancellationToken)
    {
        using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync("""
            INSERT INTO event_log (event_id, event_type, event_data, occurred_at)
            VALUES (@EventId, @EventType, @EventData::json, @OccurredAt)
            """, entry);
    }
}

internal sealed record EventLogEntity(Guid EventId, string EventType, string EventData, DateTimeOffset OccurredAt);
