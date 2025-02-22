using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BeselerNet.Api.Core;

[JsonPolymorphic]
internal abstract partial record DomainEvent(bool PublishToOutbox = false)
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string? TraceId { get; init; } = Activity.Current?.Id ?? Activity.Current?.ParentId;
}
