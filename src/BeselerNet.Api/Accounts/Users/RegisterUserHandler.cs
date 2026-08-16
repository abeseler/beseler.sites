using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Shared.Contracts.Users;
using Microsoft.AspNetCore.Identity;

namespace BeselerNet.Api.Accounts.Users;

internal static class RegisterUserHandler
{
    public static async Task<IResult> Handle(RegisterUserRequest request, AccountDataSource accounts, RoleDataSource roles, IPasswordHasher<Account> passwordHasher, CancellationToken cancellationToken)
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

        var member = await roles.WithName("member", cancellationToken);
        if (member is null)
        {
            return TypedResults.Problem("Default role is not configured.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var accountId = await accounts.NextId(cancellationToken);
        var secretHash = passwordHasher.HashPassword(default!, request.Password!);
        account = Account.CreateUser(accountId, request.Email, secretHash, request.Email, request.GivenName, request.FamilyName);
        account.AssignRole(member, "owned", account.AccountId);

        await accounts.SaveChanges(account, cancellationToken);

        return TypedResults.Created($"/v1/accounts/users/{accountId}");
    }
}
