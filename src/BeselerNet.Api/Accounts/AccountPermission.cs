using System.Security.Claims;

namespace BeselerNet.Api.Accounts;

internal sealed record AccountPermission
{
    public int AccountId { get; init; }
    public int PermissionId { get; init; }
    public required string Resource { get; init; }
    public required string Action { get; init; }
    public required string Scope { get; init; }
    public DateTimeOffset GrantedAt { get; init; }
    public int GrantedByAccountId { get; init; }
    public Claim ToClaim() => new($"{Resource}:{Action}", Scope);
}
