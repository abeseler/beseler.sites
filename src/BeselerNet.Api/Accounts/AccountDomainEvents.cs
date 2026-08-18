using BeselerNet.Api.Accounts;
using BeselerNet.Api.Core;
using System.Text.Json.Serialization;

namespace BeselerNet.Api.Core
{

    [JsonDerivedType(typeof(AccountCreated), "account-created")]
    [JsonDerivedType(typeof(AccountInvited), "account-invited")]
    [JsonDerivedType(typeof(AccountInviteAccepted), "account-invite-accepted")]
    [JsonDerivedType(typeof(AccountEmailVerified), "account-email-verified")]
    [JsonDerivedType(typeof(AccountPasswordChanged), "account-password-changed")]
    [JsonDerivedType(typeof(AccountNameChanged), "account-name-changed")]
    [JsonDerivedType(typeof(AccountDisabled), "account-disabled")]
    [JsonDerivedType(typeof(AccountEnabled), "account-enabled")]
    [JsonDerivedType(typeof(AccountUnlocked), "account-unlocked")]
    [JsonDerivedType(typeof(AccountLoginSucceeded), "account-login-succeeded")]
    [JsonDerivedType(typeof(AccountLoginFailed), "account-login-failed")]
    [JsonDerivedType(typeof(AccountPermissionGranted), "account-permission-granted")]
    [JsonDerivedType(typeof(AccountPermissionRevoked), "account-permission-revoked")]
    [JsonDerivedType(typeof(AccountRoleAssigned), "account-role-assigned")]
    [JsonDerivedType(typeof(AccountRoleRevoked), "account-role-revoked")]
    internal abstract partial record DomainEvent;
}

namespace BeselerNet.Api.Accounts
{
    internal sealed record AccountCreated(int AccountId, AccountType Type, string Username, string? Email, string SecretHash, string? GivenName, string? FamilyName) : DomainEvent;
    internal sealed record AccountInvited(int AccountId, string? Email) : DomainEvent;
    internal sealed record AccountInviteAccepted(int AccountId) : DomainEvent;
    internal sealed record AccountEmailVerified(int AccountId, string Email) : DomainEvent;
    internal sealed record AccountPasswordChanged(int AccountId, string SecretHash) : DomainEvent;
    internal sealed record AccountNameChanged(int AccountId, string GivenName, string FamilyName) : DomainEvent;
    internal sealed record AccountDisabled(int AccountId) : DomainEvent;
    internal sealed record AccountEnabled(int AccountId) : DomainEvent;
    internal sealed record AccountUnlocked(int AccountId) : DomainEvent;
    internal sealed record AccountLoginSucceeded(int AccountId) : DomainEvent;
    internal sealed record AccountLoginFailed(int AccountId, int Attempt, bool Locked) : DomainEvent;
    internal sealed record AccountPermissionGranted(int AccountId, int PermissionId, string Resource, string Action, string Scope, int GrantedByAccountId, DateTimeOffset GrantedAt) : DomainEvent;
    internal sealed record AccountPermissionRevoked(int AccountId, int PermissionId, string Resource, string Action, string Scope, int RevokedByAccountId, DateTimeOffset RevokedAt) : DomainEvent;
    internal sealed record AccountRoleAssigned(int AccountId, int RoleId, string RoleName, string Scope, int GrantedByAccountId, DateTimeOffset GrantedAt) : DomainEvent;
    internal sealed record AccountRoleRevoked(int AccountId, int RoleId, string RoleName, string Scope, int RevokedByAccountId, DateTimeOffset RevokedAt) : DomainEvent;
}
