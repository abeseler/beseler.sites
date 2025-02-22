using System.ComponentModel;

namespace BeselerNet.Api.Communications;

internal sealed class Communication : IChangeTracking
{
    private Communication() { }
    public Guid CommunicationId { get; init; }
    public int AccountId { get; init; }
    public CommunicationType Type { get; init; } = CommunicationType.Email;
    public required string Name { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? OpenedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public string? Error { get; private set; }
    public bool IsChanged { get; private set; }
    public void AcceptChanges() => IsChanged = false;
    public static Communication Create(Guid communicationId, int accountId, CommunicationType type, string name) => new()
    {
        CommunicationId = communicationId,
        AccountId = accountId,
        Type = type,
        Name = name,
        IsChanged = true
    };
}

internal enum CommunicationType
{
    Email
}
