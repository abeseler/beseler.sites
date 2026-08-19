using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Shared;
using BeselerNet.Shared.Contracts.Users;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts;

internal static class AccountHandlers
{
    public static async Task<IResult> List(ClaimsPrincipal user, AccountDataSource accounts, CancellationToken cancellationToken)
    {
        if (AccountProblems.Forbid(user, new AccountResource(), Actions.Read) is { } denied)
            return denied;

        var users = await accounts.ListUsers(cancellationToken);
        return TypedResults.Ok(users.Select(Map).ToArray());
    }

    public static async Task<IResult> Get(int accountId, ClaimsPrincipal user, AccountDataSource accounts, CancellationToken cancellationToken)
    {
        var account = await accounts.WithId_IncludeRoles(accountId, cancellationToken);
        if (account is null)
            return TypedResults.Problem(AccountProblems.NotFound);

        if (AccountProblems.Forbid(user, account, Actions.Read) is { } denied)
            return denied;

        return TypedResults.Ok(Map(account));
    }

    public static async Task<IResult> Update(int accountId, UpdateAccountRequest request, ClaimsPrincipal user, AccountDataSource accounts, CancellationToken cancellationToken)
    {
        if (request.IsInvalid(out var errors))
            return TypedResults.ValidationProblem(errors);

        var account = await accounts.WithId_IncludeRoles(accountId, cancellationToken);
        if (account is null)
            return TypedResults.Problem(AccountProblems.NotFound);

        if (AccountProblems.Forbid(user, account, Actions.Update) is { } denied)
            return denied;

        account.ChangeName(request.GivenName.Trim(), request.FamilyName.Trim());
        await accounts.SaveChanges(account, cancellationToken);
        return TypedResults.Ok(Map(account));
    }

    public static Task<IResult> Disable(int accountId, ClaimsPrincipal user, AccountDataSource accounts, CancellationToken cancellationToken) =>
        SetStatus(accountId, user, accounts, disable: true, unlock: false, cancellationToken);

    public static Task<IResult> Enable(int accountId, ClaimsPrincipal user, AccountDataSource accounts, CancellationToken cancellationToken) =>
        SetStatus(accountId, user, accounts, disable: false, unlock: false, cancellationToken);

    public static Task<IResult> Unlock(int accountId, ClaimsPrincipal user, AccountDataSource accounts, CancellationToken cancellationToken) =>
        SetStatus(accountId, user, accounts, disable: null, unlock: true, cancellationToken);

    public static async Task<IResult> SetRoles(int accountId, SetAccountRolesRequest request, ClaimsPrincipal user, AccountDataSource accounts, RoleDataSource roles, CancellationToken cancellationToken)
    {
        if (request.IsInvalid(out var errors))
            return TypedResults.ValidationProblem(errors);

        if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var callerId))
            return TypedResults.Unauthorized();

        var account = await accounts.WithId_IncludeRoles(accountId, cancellationToken);
        if (account is null)
            return TypedResults.Problem(AccountProblems.NotFound);

        if (AccountProblems.Forbid(user, account, Actions.Update, Scopes.Global) is { } denied)
            return denied;

        if (IsSelf(user, accountId))
            return TypedResults.Problem(AccountProblems.CannotChangeSelf);

        var catalog = await roles.List(cancellationToken);
        var byId = catalog.ToDictionary(role => role.RoleId);
        if (request.Roles.Any(assignment => !byId.ContainsKey(assignment.RoleId)))
            return TypedResults.Problem(AccountProblems.UnknownRoles);

        var admin = catalog.FirstOrDefault(role => string.Equals(role.Name, Roles.Admin, StringComparison.OrdinalIgnoreCase));
        var hadAdmin = account.Roles.Any(role => string.Equals(role.Name, Roles.Admin, StringComparison.OrdinalIgnoreCase));
        var keepsAdmin = admin is not null && request.Roles.Any(assignment => assignment.RoleId == admin.RoleId);
        if (hadAdmin && !keepsAdmin && !await roles.IsAssignedToAnyoneElse(Roles.Admin, accountId, cancellationToken))
            return TypedResults.Problem(AccountProblems.LastAdmin);

        var requested = request.Roles.Select(assignment => assignment.RoleId).ToHashSet();
        foreach (var existing in account.Roles.Where(role => !requested.Contains(role.RoleId)).ToList())
            account.RevokeRole(new Role { RoleId = existing.RoleId, Name = existing.Name }, callerId);

        foreach (var assignment in request.Roles)
        {
            var listed = byId[assignment.RoleId];
            account.AssignRole(new Role { RoleId = listed.RoleId, Name = listed.Name }, assignment.Scope.Trim().ToLowerInvariant(), callerId);
        }

        await accounts.SaveChanges(account, cancellationToken);
        return TypedResults.Ok(Map(account));
    }

    private static async Task<IResult> SetStatus(int accountId, ClaimsPrincipal user, AccountDataSource accounts, bool? disable, bool unlock, CancellationToken cancellationToken)
    {
        var account = await accounts.WithId_IncludeRoles(accountId, cancellationToken);
        if (account is null)
            return TypedResults.Problem(AccountProblems.NotFound);

        if (AccountProblems.Forbid(user, account, Actions.Update, Scopes.Global) is { } denied)
            return denied;

        if (IsSelf(user, accountId))
            return TypedResults.Problem(AccountProblems.CannotChangeSelf);

        if (disable is true)
            account.Disable();
        else if (disable is false)
            account.Enable();

        if (unlock)
            account.Unlock();

        await accounts.SaveChanges(account, cancellationToken);
        return TypedResults.Ok(Map(account));
    }

    public static async Task<IResult> Delete(int accountId, ClaimsPrincipal user, AccountDataSource accounts, RoleDataSource roles, CancellationToken cancellationToken)
    {
        var account = await accounts.WithId_IncludeRoles(accountId, cancellationToken);
        if (account is null)
            return TypedResults.Problem(AccountProblems.NotFound);

        if (AccountProblems.Forbid(user, account, Actions.Delete, Scopes.Global) is { } denied)
            return denied;

        if (IsSelf(user, accountId))
            return TypedResults.Problem(AccountProblems.CannotChangeSelf);

        if (account.Type != AccountType.User)
            return TypedResults.Problem(AccountProblems.CannotDeleteService);

        var isAdmin = account.Roles.Any(role => string.Equals(role.Name, Roles.Admin, StringComparison.OrdinalIgnoreCase));
        if (isAdmin && !await roles.IsAssignedToAnyoneElse(Roles.Admin, accountId, cancellationToken))
            return TypedResults.Problem(AccountProblems.LastAdmin);

        await accounts.DeleteUser(accountId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static bool IsSelf(ClaimsPrincipal user, int accountId) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var callerId) && callerId == accountId;

    internal static AccountResponse Map(Account account) => new()
    {
        AccountId = account.AccountId,
        Username = account.Username,
        Email = account.Email,
        EmailVerified = account.EmailVerifiedAt.HasValue,
        GivenName = account.GivenName,
        FamilyName = account.FamilyName,
        Name = account.Name,
        Type = account.Type.ToString(),
        Disabled = account.IsDisabled,
        Locked = account.IsLocked,
        Invited = account.IsInvited,
        LastLogon = account.LastLogon,
        CreatedAt = account.CreatedAt,
        Roles = [.. account.Roles
            .Select(role => new AccountRoleResponse { RoleId = role.RoleId, Name = role.Name, Scope = role.Scope })
            .OrderBy(role => role.Name)]
    };
}
