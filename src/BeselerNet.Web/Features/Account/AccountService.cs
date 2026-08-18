using System.Net;
using BeselerNet.Shared.Contracts.OAuth;
using BeselerNet.Shared.Contracts.Users;
using BeselerNet.Web.Shared;

namespace BeselerNet.Web.Features.Account;

internal sealed class AccountService(ApiClient api, AccountSession session)
{
    private const string ClientId = "beseler-net-web";

    private readonly ApiClient _api = api;
    private readonly AccountSession _session = session;

    public async Task<ApiResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var result = await _api.PostAsync<OAuthTokenResponse>("/v1/accounts/oauth/tokens", new OAuthTokenRequest
        {
            GrantType = OAuthGrantType.password,
            Username = email,
            Password = password,
            ClientId = ClientId
        }, cancellationToken: cancellationToken);

        if (result.StatusCode is HttpStatusCode.Unauthorized)
            return ApiResult.Fail("Email or password is wrong.", status: result.StatusCode);

        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Value?.AccessToken))
            return result.Value is null && result.Succeeded
                ? ApiResult.Fail("The API returned an empty token response.")
                : result.WithoutValue();

        await _session.StartAsync(result.Value.AccessToken, cancellationToken);
        return ApiResult.Ok();
    }

    public async Task<ApiResult> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _api.PostAsync("/v1/accounts/register-user", request, cancellationToken: cancellationToken);
        if (result.StatusCode is HttpStatusCode.Created)
            return await LoginAsync(request.Email, request.Password, cancellationToken);

        return result;
    }

    public async Task<ApiResult> ConfirmEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        var result = await _api.PostAsync<OAuthTokenResponse>("/v1/accounts/confirm-email", bearer: token, cancellationToken: cancellationToken);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Value?.AccessToken))
            return result.WithoutValue();

        await _session.StartAsync(result.Value.AccessToken, cancellationToken);
        return ApiResult.Ok();
    }

    public Task<ApiResult> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default) =>
        _api.PostAsync("/v1/accounts/forgot-password", new ForgotPasswordRequest { Email = email }, cancellationToken: cancellationToken);

    public Task<ApiResult> ResetPasswordAsync(string token, string password, CancellationToken cancellationToken = default) =>
        _api.PostAsync("/v1/accounts/reset-password", new ResetPasswordRequest { Password = password }, bearer: token, cancellationToken: cancellationToken);

    public async Task<ApiResult> ResendVerificationAsync(CancellationToken cancellationToken = default)
    {
        if (!_session.HasSession)
            return ApiResult.Fail("Sign in first so we know which address to write to.");

        var result = await _api.PostAsync("/v1/accounts/resend-email-confirmation", session: true, cancellationToken: cancellationToken);
        if (result.StatusCode is HttpStatusCode.Unauthorized)
            return ApiResult.Fail("Your session expired. Sign in again, then resend.", status: result.StatusCode);

        return result;
    }
}
