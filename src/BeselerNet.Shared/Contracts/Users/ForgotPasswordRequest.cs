namespace BeselerNet.Shared.Contracts.Users;

public sealed record ForgotPasswordRequest
{
    public required string Email { get; init; }
}
