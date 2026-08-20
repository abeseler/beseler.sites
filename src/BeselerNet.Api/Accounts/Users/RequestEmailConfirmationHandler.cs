using BeselerNet.Api.Accounts.OAuth;
using BeselerNet.Api.Communications;
using BeselerNet.Shared;
using BeselerNet.Shared.Contracts;
using BeselerNet.Shared.Contracts.Users;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BeselerNet.Api.Accounts.Users;

internal static class RequestEmailConfirmationHandler
{
    public static async Task<IResult> Handle(
        ForgotPasswordRequest request,
        AccountDataSource accounts,
        JwtGenerator tokens,
        CommunicationService communicationService,
        CancellationToken cancellationToken)
    {
        if (request.IsInvalid(out var errors))
            return TypedResults.ValidationProblem(errors);

        var account = await accounts.WithEmail(request.Email!, cancellationToken);
        if (account is { Email: not null, EmailVerifiedAt: null, IsDisabled: false, IsInvited: false })
        {
            var subjectClaim = new Claim(JwtRegisteredClaimNames.Sub, account.AccountId.ToString(), ClaimValueTypes.Integer);
            var emailClaim = new Claim(JwtRegisteredClaimNames.Email, account.Email);
            var emailVerifiedClaim = new Claim(JwtRegisteredClaimNames.EmailVerified, "true", ClaimValueTypes.Boolean);
            var token = tokens.Generate(subjectClaim, AuthLimits.ConfirmEmail, [emailClaim, emailVerifiedClaim]);
            var sent = await communicationService.SendEmailVerification(account.AccountId, account.Email, account.Name, token.AccessToken, cancellationToken);
            if (sent.Failed(out _))
            {
                return TypedResults.Accepted((string?)null, new GenericMessageResponse
                {
                    Message = "If that address still needs confirming, a new link is on the way."
                });
            }
        }

        return TypedResults.Accepted((string?)null, new GenericMessageResponse
        {
            Message = "If that address still needs confirming, a new link is on the way."
        });
    }
}
