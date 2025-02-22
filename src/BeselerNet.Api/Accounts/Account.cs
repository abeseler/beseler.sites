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
    public DateTimeOffset? EmailVerifiedAt { get; private set; }
    public string SecretHash { get; private set; } = default!;
    public DateTimeOffset SecretHashedAt { get; private set; }
    public string? GivenName { get; private set; }
    public string? FamilyName { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? DisabledAt { get; private set; }
    public DateTimeOffset? LockedAt { get; private set; }
    public DateTimeOffset? LastLogon { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public string Name => this switch
    {
        { GivenName: not null, FamilyName: not null } => $"{GivenName} {FamilyName}",
        { GivenName: not null } => GivenName,
        { FamilyName: not null } => FamilyName,
        _ => Username,
    };
    public bool IsDisabled => DisabledAt.HasValue;
    public bool IsLocked => LockedAt.HasValue;
    public IReadOnlyCollection<DomainEvent> UncommittedEvents => _events ?? [];
    public bool IsChanged { get; private set; }
    public static Account CreateUser(int accountId, string username, string secretHash, string email, string givenName, string familyName) =>
        Create(new AccountCreated(accountId, AccountType.User, username, email, secretHash, givenName, familyName));
    private static Account Create(AccountCreated domainEvent) => new()
    {
        AccountId = domainEvent.AccountId,
        Type = domainEvent.Type,
        Username = domainEvent.Username,
        SecretHash = domainEvent.SecretHash!,
        SecretHashedAt = domainEvent.OccurredAt,
        Email = domainEvent.Email,
        GivenName = domainEvent.GivenName,
        FamilyName = domainEvent.FamilyName,
        CreatedAt = domainEvent.OccurredAt,
        IsChanged = true,
        _events = [domainEvent]
    };
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
            LockedAt = DateTimeOffset.UtcNow;
        }
        IsChanged = true;
        _events ??= [];
        _events.Add(new AccountLoginFailed(AccountId, FailedLoginAttempts, IsLocked));
    }
    public void VerifyEmail(string email)
    {
        Email = email;
        EmailVerifiedAt = DateTimeOffset.UtcNow;
        IsChanged = true;
        _events ??= [];
        _events.Add(new AccountEmailVerified(AccountId, email));
    }
    public void ResetPassword(string hash)
    {
        SecretHash = hash;
        SecretHashedAt = DateTimeOffset.UtcNow;
        IsChanged = true;
    }
    public void Disable(string? disabledBy = null)
    {
        DisabledAt = DateTimeOffset.UtcNow;
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
        if (Email is not null && EmailVerifiedAt.HasValue)
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
