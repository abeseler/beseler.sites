using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Shared;

namespace BeselerNet.Api.Accounts;

internal sealed class AccountResource : IAuthorizableResource
{
    public static string ResourceName => Resources.Account;
}
