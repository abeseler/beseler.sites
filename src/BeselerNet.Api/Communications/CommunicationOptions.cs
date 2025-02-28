namespace BeselerNet.Api.Communications;

internal sealed record CommunicationOptions
{
    public const string SectionName = "Communication";
    public string? SenderEmail { get; init; }
    public string? SenderName { get; init; }
    public string? ConfirmEmailUrl { get; init; }
    public string? ResetPasswordUrl { get; init; }
}
