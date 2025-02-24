using BeselerNet.Api.Accounts;
using BeselerNet.Api.Accounts.EventHandlers;
using BeselerNet.Api.Core;
using System.Text.Json.Serialization;

namespace BeselerNet.Api.Core
{

    [JsonDerivedType(typeof(AccountCreated), "account-created")]
    [JsonDerivedType(typeof(AccountEmailVerified), "account-email-verified")]
    [JsonDerivedType(typeof(AccountLoginSucceeded), "account-login-succeeded")]
    [JsonDerivedType(typeof(AccountLoginFailed), "account-login-failed")]
    [JsonDerivedType(typeof(AccountDisabled), "account-disabled")]
    internal abstract partial record DomainEvent;
}

namespace BeselerNet.Api.Accounts
{
    internal sealed record AccountCreated(
        int AccountId,
        AccountType Type,
        string Username,
        string? Email,
        string SecretHash,
        string? GivenName,
        string? FamilyName) : DomainEvent("account", !string.IsNullOrWhiteSpace(Email))
    {
        public override string ResourceId => AccountId.ToString();
    }
    internal sealed record AccountEmailVerified(int AccountId, string Email) : DomainEvent("account")
    {
        public override string ResourceId => AccountId.ToString();
    }
    internal sealed record AccountLoginSucceeded(int AccountId) : DomainEvent("account")
    {
        public override string ResourceId => AccountId.ToString();
    }
    internal sealed record AccountLoginFailed(int AccountId, int Attempt, bool Locked) : DomainEvent("account", Locked)
    {
        public override string ResourceId => AccountId.ToString();
    }
    internal sealed record AccountDisabled(int AccountId, string? DisabledBy) : DomainEvent("account")
    {
        public override string ResourceId => AccountId.ToString();
    }

    internal static class AccountDomainEventHandlerRegistrar
    {
        public static void AddAccountDomainEventHandlers(this IServiceCollection services)
        {
            _ = services.AddScoped<AccountCreatedHandler>();
            _ = services.AddScoped<AccountLoginFailedHandler>();
        }
    }
}
