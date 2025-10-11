using BeselerNet.Api.Accounts;
using BeselerNet.Api.Core;
using System.Text.Json.Serialization;

namespace BeselerNet.Api.Core
{

    [JsonDerivedType(typeof(AccountCreated), "account-created")]
    [JsonDerivedType(typeof(AccountEmailVerified), "account-email-verified")]
    [JsonDerivedType(typeof(AccountPasswordChanged), "account-password-changed")]
    [JsonDerivedType(typeof(AccountLoginSucceeded), "account-login-succeeded")]
    [JsonDerivedType(typeof(AccountLoginFailed), "account-login-failed")]
    [JsonDerivedType(typeof(AccountPermissionGranted), "account-permission-granted")]
    [JsonDerivedType(typeof(AccountPermissionRevoked), "account-permission-revoked")]
    internal abstract partial record DomainEvent;
}

namespace BeselerNet.Api.Accounts
{
    internal sealed record AccountCreated(int AccountId, AccountType Type, string Username, string? Email, string SecretHash, string? GivenName, string? FamilyName) : DomainEvent;
    internal sealed record AccountEmailVerified(int AccountId, string Email) : DomainEvent;
    internal sealed record AccountPasswordChanged(int AccountId, string SecretHash) : DomainEvent;
    internal sealed record AccountLoginSucceeded(int AccountId) : DomainEvent;
    internal sealed record AccountLoginFailed(int AccountId, int Attempt, bool Locked) : DomainEvent;
    internal sealed record AccountPermissionGranted(int AccountId, int PermissionId, string Resource, string Action, string Scope, int GrantedByAccountId, DateTimeOffset GrantedAt) : DomainEvent;
    internal sealed record AccountPermissionRevoked(int AccountId, int PermissionId, string Resource, string Action, string Scope, int RevokedByAccountId, DateTimeOffset RevokedAt) : DomainEvent;
}
