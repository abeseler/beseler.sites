using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BeselerNet.Api.Core;

[JsonPolymorphic]
internal abstract partial record DomainEvent(string AggregateType, string AggregateId, bool SendToOutbox = false)
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public required long Version { get; init; }
    public string? TraceId { get; init; } = Activity.Current?.Id ?? Activity.Current?.ParentId;
}
