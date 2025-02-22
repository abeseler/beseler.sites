using Dapper;
using Npgsql;

namespace BeselerNet.Api.Communications;

internal sealed class CommunicationDataSource(NpgsqlDataSource dataSource)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;

    public async Task<Communication?> WithId(Guid id, CancellationToken stoppingToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);
        return await connection.QuerySingleOrDefaultAsync<Communication>(
            "SELECT * FROM communication WHERE communication_id = @id", new { id });
    }

    public async Task SaveChanges(Communication communication, CancellationToken stoppingToken)
    {
        if (communication.IsChanged is false)
        {
            return;
        }

        using var connection = await _dataSource.OpenConnectionAsync(stoppingToken);        
        await connection.ExecuteAsync("""
            INSERT INTO communication (
                communication_id,
                account_id,
                type,
                name,
                created_at,
                processed_at,
                delivered_at,
                opened_at,
                failed_at,
                error
            ) VALUES (
                @communicationId,
                @accountId,
                @type,
                @name,
                @createdAt,
                @processedAt,
                @deliveredAt,
                @openedAt,
                @failedAt,
                @error
            )
            ON CONFLICT (communication_id) DO UPDATE SET
                processed_at = @processedAt,
                delivered_at = @deliveredAt,
                opened_at = @openedAt,
                failed_at = @failedAt,
                error = @error
            """, new
            {
                communication.CommunicationId,
                communication.AccountId,
                Type = communication.Type.ToString(),
                communication.Name,
                communication.CreatedAt,
                communication.ProcessedAt,
                communication.DeliveredAt,
                communication.OpenedAt,
                communication.FailedAt,
                communication.Error
            });

        communication.AcceptChanges();
    }
}
