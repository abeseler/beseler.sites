using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Settings;
using BeselerNet.Shared;
using BeselerNet.Shared.Contracts.Users;
using Microsoft.AspNetCore.Identity;

namespace BeselerNet.Api.Accounts.Users;

internal static class RegisterUserHandler
{
    public static async Task<IResult> Handle(RegisterUserRequest request, AccountDataSource accounts, RoleDataSource roles, SettingDataSource settings, IPasswordHasher<Account> passwordHasher, CancellationToken cancellationToken)
    {
        if (request.IsInvalid(out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        if (!await AppStatus.SignupOpen(roles, settings, cancellationToken))
            return TypedResults.Problem(AccountProblems.SignupClosed);

        var account = await accounts.WithEmail(request.Email, cancellationToken);
        if (account is not null)
        {
            errors = [];
            errors["Email"] = ["Email already exists."];
            return TypedResults.ValidationProblem(errors);
        }

        var member = await roles.WithName(Roles.Member, cancellationToken);
        var admin = await roles.WithName(Roles.Admin, cancellationToken);
        if (member is null || admin is null)
        {
            return TypedResults.Problem("Default roles are not configured.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var accountId = await accounts.NextId(cancellationToken);
        var secretHash = passwordHasher.HashPassword(default!, request.Password!);
        account = Account.CreateUser(accountId, request.Email, secretHash, request.Email, request.GivenName, request.FamilyName);
        account.AssignRole(member, Scopes.Owned, account.AccountId);

        if (!await roles.IsAssignedToAnyone(Roles.Admin, cancellationToken))
        {
            account.AssignRole(admin, Scopes.Global, account.AccountId);
            account.VerifyEmail(request.Email);
        }

        await accounts.SaveChanges(account, cancellationToken);

        return TypedResults.Created($"/v1/accounts/users/{accountId}");
    }
}
