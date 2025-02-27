using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Core;
using Microsoft.IdentityModel.JsonWebTokens;
using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace BeselerNet.Api.Accounts;

internal sealed class Account : IChangeTracking, IAuthorizableResource, IOwnedResource
{
    public static string ResourceName { get; } = "account";
    private Account() { }
    private readonly List<AccountPermission> _permissions = [];
    private readonly List<DomainEvent> _events = [];
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
    public IReadOnlyList<AccountPermission> Permissions => (_permissions ?? []).AsReadOnly();
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
    public void Login()
    {
        LastLogon = DateTimeOffset.UtcNow;
        FailedLoginAttempts = 0;
        IsChanged = true;
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
        _events.Add(new AccountLoginFailed(AccountId, FailedLoginAttempts, IsLocked));
    }
    public void VerifyEmail(string email)
    {
        Email = email;
        EmailVerifiedAt = DateTimeOffset.UtcNow;
        IsChanged = true;
        _events.Add(new AccountEmailVerified(AccountId, email));
    }
    public void ChangePassword(string hash)
    {
        SecretHash = hash;
        SecretHashedAt = DateTimeOffset.UtcNow;
        IsChanged = true;
        _events.Add(new AccountPasswordChanged(AccountId, hash));
    }
    public void Disable(string? disabledBy = null)
    {
        DisabledAt = DateTimeOffset.UtcNow;
        IsChanged = true;
        _events.Add(new AccountDisabled(AccountId, disabledBy));
    }
    public void Grant(Permission permission, string scope, int grantedBy)
    {
        var existing = _permissions.FirstOrDefault(p => p.PermissionId == permission.PermissionId);
        if (existing is null)
        {
            _permissions.Add(new AccountPermission
            {
                AccountId = AccountId,
                PermissionId = permission.PermissionId,
                Resource = permission.Resource,
                Action = permission.Action,
                Scope = scope,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedByAccountId = grantedBy
            });
            _events.Add(new AccountPermissionGranted(AccountId, permission, scope, grantedBy));
            IsChanged = true;
        }
        else if (existing.Scope != scope)
        {
            _ = _permissions.Remove(existing);
            _permissions.Add(new AccountPermission
            {
                AccountId = AccountId,
                PermissionId = permission.PermissionId,
                Resource = permission.Resource,
                Action = permission.Action,
                Scope = scope,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedByAccountId = grantedBy
            });
            _events.Add(new AccountPermissionGranted(AccountId, permission, scope, grantedBy));
            IsChanged = true;
        }
    }
    public void Revoke(Permission permission, int revokedBy)
    {
        var existing = _permissions.FirstOrDefault(p => p.PermissionId == permission.PermissionId);
        if (existing is not null)
        {
            _ = _permissions.Remove(existing);
            _events.Add(new AccountPermissionRevoked(AccountId, permission, existing.Scope, revokedBy));
            IsChanged = true;
        }
    }
    public void AcceptChanges()
    {
        _events.Clear();
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
        foreach (var claim in Permissions.Select(p => p.ToClaim()))
        {
            claims.Add(claim);
        }
        var identity = new ClaimsIdentity(claims);
        return new ClaimsPrincipal(identity);
    }

    public bool IsOwnedBy(ClaimsPrincipal user) => int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId) && accountId == AccountId;
    private static Account Create(AccountCreated domainEvent)
    {
        var account = new Account()
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
            IsChanged = true
        };
        account._events.Add(domainEvent);
        return account;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum AccountType
{
    User,
    Service,
}
