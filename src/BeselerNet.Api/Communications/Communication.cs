using System.ComponentModel;

namespace BeselerNet.Api.Communications;

internal sealed class Communication : IChangeTracking
{
    private Communication() { }
    public Guid CommunicationId { get; init; }
    public required string Provider { get; init; }
    public CommunicationType Type { get; init; } = CommunicationType.Email;
    public required string Name { get; init; }
    public string? ExternalId { get; private set; }
    public int AccountId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? OpenedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public string? Error { get; private set; }
    public bool IsChanged { get; private set; }
    public void AcceptChanges() => IsChanged = false;
    public static Communication Create(string provider, CommunicationType type, string name, int accountId) => new()
    {
        Provider = provider,
        CommunicationId = Guid.CreateVersion7(),
        AccountId = accountId,
        Type = type,
        Name = name,
        IsChanged = true
    };
    public void SetExternalId(string externalId)
    {
        ExternalId = externalId;
        IsChanged = true;
    }
    public void Sent(DateTimeOffset sentAt)
    {
        SentAt = sentAt;
        IsChanged = true;
    }
    public void Delivered(DateTimeOffset deliveredAt)
    {
        DeliveredAt = deliveredAt;
        IsChanged = true;
    }
    public void Opened(DateTimeOffset openedAt)
    {
        OpenedAt = openedAt;
        IsChanged = true;
    }
    public void Failed(DateTimeOffset failedAt, string error)
    {
        Error = Error is null ? error : $"{Error}; {error}";
        FailedAt = failedAt;
        IsChanged = true;
    }
}

internal enum CommunicationType
{
    Email
}
