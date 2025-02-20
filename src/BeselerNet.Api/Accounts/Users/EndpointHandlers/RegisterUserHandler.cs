using BeselerNet.Shared.Contracts.Users;
using Microsoft.AspNetCore.Identity;

namespace BeselerNet.Api.Accounts.Users.EndpointHandlers;

internal static class RegisterUserHandler
{
    public static async Task<IResult> Handle(RegisterUserRequest request, AccountDataSource accounts, IPasswordHasher<Account> passwordHasher, CancellationToken stoppingToken)
    {
        if (request.HasValidationErrors(out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        var account = await accounts.WithEmail(request.Email, stoppingToken);
        if (account is not null)
        {
            errors = [];
            errors["Email"] = ["Email already exists."];
            return TypedResults.ValidationProblem(errors);
        }

        var accountId = await accounts.NextId(stoppingToken);
        var secretHash = passwordHasher.HashPassword(default!, request.Password!);
        account = Account.CreateUser(accountId, request.Email, secretHash, request.Email, request.GivenName, request.FamilyName);

        await accounts.SaveChanges(account, stoppingToken);

        return TypedResults.Created($"/v1/accounts/users/{accountId}");
    }
}
