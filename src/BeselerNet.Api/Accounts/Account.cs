using BeselerNet.Api.Core;
using Microsoft.IdentityModel.JsonWebTokens;
using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace BeselerNet.Api.Accounts;

internal sealed class Account : IChangeTracking
{
    private Account() { }
    private List<DomainEvent>? _events;
    public int AccountId { get; private init; }
    public AccountType Type { get; private init; }
    public string Username { get; private set; } = default!;
    public string? Email { get; private set; }
    public DateTimeOffset? EmailVerifiedOn { get; private set; }
    public string? SecretHash { get; private set; }
    public DateTimeOffset? SecretHashedOn { get; private set; }
    public string? GivenName { get; private set; }
    public string? FamilyName { get; private set; }
    public DateTimeOffset CreatedOn { get; private init; }
    public DateTimeOffset? DisabledOn { get; private set; }
    public DateTimeOffset? LockedOn { get; private set; }
    public DateTimeOffset? LastLogon { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public int EventLogCount { get; private set; }
    public string Name => this switch
    {
        { GivenName: not null, FamilyName: not null } => $"{GivenName} {FamilyName}",
        { GivenName: not null } => GivenName,
        { FamilyName: not null } => FamilyName,
        _ => Username,
    };
    public bool IsDisabled => DisabledOn.HasValue;
    public bool IsLocked => LockedOn.HasValue;
    public IReadOnlyCollection<DomainEvent> UncommittedEvents => _events ?? [];
    public bool IsChanged { get; private set; }
    public void Login()
    {
        LastLogon = DateTimeOffset.UtcNow;
        FailedLoginAttempts = 0;
        IsChanged = true;
        _events ??= [];
        _events.Add(new AccountLoginSucceeded(AccountId));
    }
    public void FailLogin()
    {
        FailedLoginAttempts += 1;
        if (FailedLoginAttempts >= 5)
        {
            LockedOn = DateTimeOffset.UtcNow;
        }
        IsChanged = true;
        _events ??= [];
        _events.Add(new AccountLoginFailed(AccountId, FailedLoginAttempts, IsLocked));
    }
    public void ResetPassword(string hash)
    {
        SecretHash = hash;
        SecretHashedOn = DateTimeOffset.UtcNow;
        IsChanged = true;
    }
    public void Disable(string? disabledBy = null)
    {
        DisabledOn = DateTimeOffset.UtcNow;
        IsChanged = true;
        _events ??= [];
        _events.Add(new AccountDisabled(AccountId, disabledBy));
    }
    public void AcceptChanges()
    {
        _events = null;
        IsChanged = false;
    }

    public ClaimsPrincipal ToClaimsPrincipal()
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, AccountId.ToString(), ClaimValueTypes.Integer),
            new(JwtRegisteredClaimNames.Name, Name),
        };
        if (Email is not null)
        {
            claims.Add(new(JwtRegisteredClaimNames.Email, Email));
        }
        if (EmailVerifiedOn is not null)
        {
            claims.Add(new(JwtRegisteredClaimNames.EmailVerified, "true", ClaimValueTypes.Boolean));
        }
        var identity = new ClaimsIdentity(claims);
        return new ClaimsPrincipal(identity);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum AccountType
{
    User,
    Service,
}
