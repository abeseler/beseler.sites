using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Shared.Contracts.Users;
using Microsoft.AspNetCore.Identity;

namespace BeselerNet.Api.Accounts.Users;

internal static class RegisterUserHandler
{
    public static async Task<IResult> Handle(RegisterUserRequest request, AccountDataSource accounts, PermissionDataSource permissionDataSource, IPasswordHasher<Account> passwordHasher, CancellationToken cancellationToken)
    {
        if (request.IsInvalid(out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        var account = await accounts.WithEmail(request.Email, cancellationToken);
        if (account is not null)
        {
            errors = [];
            errors["Email"] = ["Email already exists."];
            return TypedResults.ValidationProblem(errors);
        }

        var accountId = await accounts.NextId(cancellationToken);
        var permissions = await permissionDataSource.GetCollection(cancellationToken);
        var secretHash = passwordHasher.HashPassword(default!, request.Password!);
        account = Account.CreateUser(accountId, request.Email, secretHash, request.Email, request.GivenName, request.FamilyName);

        foreach (var permission in DefaultPermissions(permissions).Where(x => x is not null))
        {
            account.Grant(permission!, "owned", account.AccountId);
        }

        await accounts.SaveChanges(account, cancellationToken);

        return TypedResults.Created($"/v1/accounts/users/{accountId}");
    }

    private static IEnumerable<Permission?> DefaultPermissions(PermissionCollecton permissions)
    {
        yield return permissions.Get(Account.ResourceName, "read");
        yield return permissions.Get(Account.ResourceName, "update");
        yield return permissions.Get(Account.ResourceName, "delete");
    }
}
