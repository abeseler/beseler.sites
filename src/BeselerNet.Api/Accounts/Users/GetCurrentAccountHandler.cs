using BeselerNet.Shared.Contracts.Users;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users;

internal static class GetCurrentAccountHandler
{
    public static async Task<IResult> Handle(ClaimsPrincipal principal, AccountDataSource accounts, CancellationToken cancellationToken)
    {
        if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var accountId))
        {
            return TypedResults.Unauthorized();
        }

        var account = await accounts.WithId_IncludePermissions(accountId, cancellationToken);
        if (account is null)
        {
            return TypedResults.Unauthorized();
        }

        var problem = account switch
        {
            { IsDisabled: true } => AccountProblems.Disabled,
            { IsLocked: true } => AccountProblems.Locked,
            _ => null
        };

        if (problem is not null)
        {
            return TypedResults.Problem(problem);
        }

        return TypedResults.Ok(Map(account));
    }

    private static AccountProfileResponse Map(Account account)
    {
        var permissions = account.Permissions
            .Concat(account.RolePermissions)
            .GroupBy(permission => (permission.Resource, permission.Action))
            .Select(group => new AccountPermissionResponse
            {
                Resource = group.Key.Resource,
                Action = group.Key.Action,
                Scopes = [.. group.Select(permission => permission.Scope).Distinct()]
            })
            .OrderBy(permission => permission.Resource)
            .ThenBy(permission => permission.Action)
            .ToArray();

        var roles = account.Roles
            .Select(role => new AccountRoleResponse
            {
                Name = role.Name,
                Scope = role.Scope
            })
            .OrderBy(role => role.Name)
            .ToArray();

        return new AccountProfileResponse
        {
            AccountId = account.AccountId,
            Username = account.Username,
            Email = account.Email,
            EmailVerified = account.EmailVerifiedAt.HasValue,
            GivenName = account.GivenName,
            FamilyName = account.FamilyName,
            Name = account.Name,
            Roles = roles,
            Permissions = permissions
        };
    }
}
