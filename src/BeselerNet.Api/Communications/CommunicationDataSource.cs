using Dapper;
using Npgsql;

namespace BeselerNet.Api.Communications;

internal sealed class CommunicationDataSource(NpgsqlDataSource dataSource)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;

    public async Task<Communication?> WithId(Guid id, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Communication>(
            "SELECT * FROM communication WHERE communication_id = @id", new { id });
    }

    public async Task SaveChanges(Communication communication, CancellationToken cancellationToken)
    {
        if (communication.IsChanged is false)
        {
            return;
        }

        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        _ = await connection.ExecuteAsync("""
            INSERT INTO communication (
                communication_id,
                provider,
                type,
                name,
                external_id,
                account_id,
                created_at,
                sent_at,
                delivered_at,
                opened_at,
                failed_at,
                error
            ) VALUES (
                @communicationId,
                @provider,
                @type,
                @name,
                @externalId,
                @accountId,
                @createdAt,
                @sentAt,
                @deliveredAt,
                @openedAt,
                @failedAt,
                @error
            )
            ON CONFLICT (communication_id) DO UPDATE SET
                external_id = @externalId,
                sent_at = @sentAt,
                delivered_at = @deliveredAt,
                opened_at = @openedAt,
                failed_at = @failedAt,
                error = @error
            """, new
        {
            communication.CommunicationId,
            communication.Provider,
            Type = communication.Type.ToString(),
            communication.Name,
            communication.ExternalId,
            communication.AccountId,
            communication.CreatedAt,
            communication.SentAt,
            communication.DeliveredAt,
            communication.OpenedAt,
            communication.FailedAt,
            communication.Error
        });

        communication.AcceptChanges();
    }
}
