using BeselerNet.Api.Accounts;
using BeselerNet.Api.Accounts.EventHandlers;
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
    internal sealed record AccountCreated(int AccountId, AccountType Type, string Username, string? Email, string SecretHash, string? GivenName, string? FamilyName)
        : DomainEvent("account", AccountId.ToString(), !string.IsNullOrWhiteSpace(Email));
    internal sealed record AccountEmailVerified(int AccountId, string Email)
        : DomainEvent("account", AccountId.ToString());
    internal sealed record AccountPasswordChanged(int AccountId, string SecretHash)
        : DomainEvent("account", AccountId.ToString());
    internal sealed record AccountLoginSucceeded(int AccountId)
        : DomainEvent("account", AccountId.ToString());
    internal sealed record AccountLoginFailed(int AccountId, int Attempt, bool Locked)
        : DomainEvent("account", AccountId.ToString(), Locked);
    internal sealed record AccountPermissionGranted(int AccountId, int PermissionId, string Resource, string Action, string Scope, int GrantedByAccountId)
        : DomainEvent("account", AccountId.ToString());
    internal sealed record AccountPermissionRevoked(int AccountId, int PermissionId, string Resource, string Action, string Scope, int RevokedByAccountId)
        : DomainEvent("account", AccountId.ToString());

    internal static class AccountDomainEventHandlerRegistrar
    {
        public static void AddAccountDomainEventHandlers(this IServiceCollection services)
        {
            _ = services.AddScoped<AccountCreatedHandler>();
            _ = services.AddScoped<AccountLoginFailedHandler>();
        }
    }
}
