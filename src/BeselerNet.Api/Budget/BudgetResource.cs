using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Shared;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Budget;

internal sealed class BudgetResource(int accountId) : IAuthorizableResource, IOwnedResource
{
    public static string ResourceName => Resources.Budget;

    public bool IsOwnedBy(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) && id == accountId;
}
