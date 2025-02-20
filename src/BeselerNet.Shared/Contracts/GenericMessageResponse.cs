namespace BeselerNet.Shared.Contracts;

public sealed record GenericMessageResponse
{
    public required string Message { get; init; }
}
