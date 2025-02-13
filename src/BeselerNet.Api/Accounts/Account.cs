using System.ComponentModel;

namespace BeselerNet.Api.Accounts;

internal sealed class Account : IChangeTracking
{
    private Account() { }
    public int Id { get; private init; }
    public string Username { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? SecretHash { get; private set; }
    public DateTime? SecretHashedOn { get; private set; }
    public string? GivenName { get; private set; }
    public string? FamilyName { get; private set; }
    public DateTime CreatedOn { get; private init; }
    public DateTime? DisabledOn { get; private set; }
    public DateTime? LockedOn { get; private set; }
    public DateTime? LastLogon { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public bool IsDisabled => DisabledOn.HasValue;
    public bool IsLocked => LockedOn.HasValue;
    public bool IsChanged { get; private set; }
    public void AcceptChanges() => IsChanged = false;
}
