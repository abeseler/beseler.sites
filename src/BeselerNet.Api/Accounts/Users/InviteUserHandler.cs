using System.Security.Cryptography;
using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Communications;
using BeselerNet.Shared;
using BeselerNet.Shared.Contracts.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users;

internal static class InviteUserHandler
{
    public const string InviteClaim = "invite";

    public static async Task<IResult> Handle(
        InviteUserRequest request,
        ClaimsPrincipal user,
        AccountDataSource accounts,
        RoleDataSource roles,
        IPasswordHasher<Account> passwordHasher,
        JwtGenerator tokens,
        CommunicationService mail,
        CancellationToken cancellationToken)
    {
        if (AccountProblems.Forbid(user, new AccountResource(), Actions.Update, Scopes.Global) is { } denied)
            return denied;

        if (request.IsInvalid(out var errors))
            return TypedResults.ValidationProblem(errors);

        if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var callerId))
            return TypedResults.Unauthorized();

        if (await accounts.WithEmail(request.Email, cancellationToken) is not null)
            return TypedResults.Problem(AccountProblems.EmailTaken);

        var member = await roles.WithName(Roles.Member, cancellationToken);
        if (member is null)
            return TypedResults.Problem("Default roles are not configured.", statusCode: StatusCodes.Status500InternalServerError);

        var catalog = await roles.List(cancellationToken);
        var byId = catalog.ToDictionary(role => role.RoleId);
        var assignments = request.Roles.Count == 0
            ? [new AccountRoleAssignment { RoleId = member.RoleId, Scope = Scopes.Owned }]
            : request.Roles;

        if (assignments.Any(assignment => !byId.ContainsKey(assignment.RoleId)))
            return TypedResults.Problem(AccountProblems.UnknownRoles);

        var secretHash = passwordHasher.HashPassword(default!, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
        var accountId = await accounts.NextId(cancellationToken);
        var account = Account.CreateUser(accountId, request.Email.Trim(), secretHash, request.Email.Trim(), request.GivenName.Trim(), request.FamilyName.Trim());
        account.MarkInvited();

        foreach (var assignment in assignments)
        {
            var listed = byId[assignment.RoleId];
            account.AssignRole(new Role { RoleId = listed.RoleId, Name = listed.Name }, assignment.Scope.Trim().ToLowerInvariant(), callerId);
        }

        await accounts.SaveChanges(account, cancellationToken);
        account = await accounts.WithId_IncludeRoles(accountId, cancellationToken) ?? account;

        var send = await SendInvite(account, tokens, mail, cancellationToken);
        if (send.Failed(out var exception))
            throw exception;

        return TypedResults.Created($"/v1/accounts/{accountId}", AccountHandlers.Map(account));
    }

    public static async Task<IResult> Resend(
        int accountId,
        ClaimsPrincipal user,
        AccountDataSource accounts,
        JwtGenerator tokens,
        CommunicationService mail,
        CancellationToken cancellationToken)
    {
        var account = await accounts.WithId_IncludeRoles(accountId, cancellationToken);
        if (account is null)
            return TypedResults.Problem(AccountProblems.NotFound);

        if (AccountProblems.Forbid(user, account, Actions.Update, Scopes.Global) is { } denied)
            return denied;

        if (!account.IsInvited)
            return TypedResults.Problem(AccountProblems.NotInvited);

        if (account.IsDisabled)
            return TypedResults.Problem(AccountProblems.Disabled);

        var send = await SendInvite(account, tokens, mail, cancellationToken);
        if (send.Failed(out var exception))
            throw exception;

        return TypedResults.Accepted((string?)null);
    }

    public static async Task<BeselerNet.Shared.Core.Result> SendInvite(Account account, JwtGenerator tokens, CommunicationService mail, CancellationToken cancellationToken)
    {
        if (account.Email is null)
            return new InvalidOperationException("Invited account has no email.");

        var subject = new Claim(JwtRegisteredClaimNames.Sub, account.AccountId.ToString(), ClaimValueTypes.Integer);
        var email = new Claim(JwtRegisteredClaimNames.Email, account.Email);
        var invite = new Claim(InviteClaim, "true", ClaimValueTypes.Boolean);
        var token = tokens.Generate(subject, AuthLimits.Invite, [email, invite]).AccessToken;
        var sent = await mail.SendInvite(account.AccountId, account.Email, account.Name, token, cancellationToken);
        return sent.Failed(out var exception) ? exception : BeselerNet.Shared.Core.Result.Success;
    }
}
