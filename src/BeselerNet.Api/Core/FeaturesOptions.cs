namespace BeselerNet.Api.Core;

public sealed record FeaturesOptions
{
    public const string SectionName = "Features";
    public bool OutboxEnabled { get; init; }
}
