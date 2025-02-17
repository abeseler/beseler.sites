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
        string? SecretHash,
        string? GivenName,
        string? FamilyName) : DomainEvent(!string.IsNullOrWhiteSpace(Email));

    internal sealed record AccountEmailVerified(int AccountId, string Email) : DomainEvent;
    internal sealed record AccountLoginSucceeded(int AccountId) : DomainEvent;
    internal sealed record AccountLoginFailed(int AccountId, int Attempt, bool Locked) : DomainEvent(Locked);
    internal sealed record AccountDisabled(int AccountId, string? DisabledBy) : DomainEvent;

    internal static class AccountDomainEventHandlerRegistrar
    {
        public static void AddAccountDomainEventHandlers(this IServiceCollection services)
        {
            services.AddScoped<AccountCreatedHandler>();
            services.AddScoped<AccountLoginFailedHandler>();
        }
    }
}
