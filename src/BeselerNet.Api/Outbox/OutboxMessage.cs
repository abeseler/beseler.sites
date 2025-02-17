namespace BeselerNet.Api.Outbox;

internal sealed record OutboxMessage
{
    public Guid MessageId { get; init; }
    public required string MessageType { get; init; }
    public required string MessageData { get; init; }
    public DateTimeOffset InvisibleUntil { get; init; }
    public int ReceivesRemaining { get; init; }
}
