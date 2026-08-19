using System.Net;
using BeselerNet.Shared;
using BeselerNet.Shared.Contracts.OAuth;
using BeselerNet.Shared.Contracts.Users;
using BeselerNet.Web.Shared;
using Microsoft.Extensions.Options;

namespace BeselerNet.Web.Features.Account;

internal sealed class AccountService(ApiClient api, AccountSession session, IOptions<OAuthOptions> oauth)
{
    private readonly ApiClient _api = api;
    private readonly AccountSession _session = session;
    private readonly OAuthOptions _oauth = oauth.Value;

    public async Task<ApiResult<string>> LoginAsync(string email, string password, bool persist = false, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var result = await _api.PostAsync<OAuthTokenResponse>("/v1/accounts/oauth/tokens", new OAuthTokenRequest
        {
            GrantType = OAuthGrantType.password,
            Username = email,
            Password = password,
            ClientId = _oauth.WebClientId,
            ClientSecret = _oauth.WebClientSecret
        }, cancellationToken: cancellationToken);

        if (result.IsLocked)
            return ApiResult<string>.Fail(result.Error ?? "Your account is locked. Reset your password to unlock it.", result.FieldErrors, result.StatusCode, result.Title);

        if (result.StatusCode is HttpStatusCode.Unauthorized)
            return ApiResult<string>.Fail("Email or password is wrong.", status: result.StatusCode);

        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Value?.AccessToken))
            return result.Value is null && result.Succeeded
                ? ApiResult<string>.Fail("The API returned an empty token response.")
                : ApiResult<string>.Fail(result.Error ?? "Sign in failed.", result.FieldErrors, result.StatusCode, result.Title);

        var handoff = await _session.EstablishAsync(result.Value, persist, returnUrl, cancellationToken);
        return ApiResult<string>.Ok(handoff);
    }

    public async Task<ApiResult<string>> RegisterAsync(RegisterUserRequest request, bool persist = false, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var result = await _api.PostAsync("/v1/accounts/register-user", request, cancellationToken: cancellationToken);
        if (result.StatusCode is HttpStatusCode.Created)
            return await LoginAsync(request.Email, request.Password, persist, returnUrl, cancellationToken);

        return ApiResult<string>.Fail(result.Error ?? "Could not create the account.", result.FieldErrors, result.StatusCode);
    }

    public async Task<ApiResult<string>> ConfirmEmailAsync(string token, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var result = await _api.PostAsync<OAuthTokenResponse>("/v1/accounts/confirm-email", bearer: token, cancellationToken: cancellationToken);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Value?.AccessToken))
            return ApiResult<string>.Fail(result.Error ?? "Could not confirm email.", result.FieldErrors, result.StatusCode);

        var handoff = await _session.EstablishAsync(result.Value, _session.Persist, returnUrl, cancellationToken);
        return ApiResult<string>.Ok(handoff);
    }

    public Task<ApiResult> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default) =>
        _api.PostAsync("/v1/accounts/forgot-password", new ForgotPasswordRequest { Email = email }, cancellationToken: cancellationToken);

    public async Task<ApiResult<string>> AcceptInviteAsync(string token, string password, bool persist = false, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var result = await _api.PostAsync<OAuthTokenResponse>("/v1/accounts/accept-invite", new ResetPasswordRequest { Password = password }, bearer: token, cancellationToken: cancellationToken);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Value?.AccessToken))
            return ApiResult<string>.Fail(result.Error ?? "Could not accept the invite.", result.FieldErrors, result.StatusCode);

        var handoff = await _session.EstablishAsync(result.Value, persist, returnUrl, cancellationToken);
        return ApiResult<string>.Ok(handoff);
    }

    public async Task<ApiResult<string>> ResetPasswordAsync(string token, string password, bool persist = false, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var result = await _api.PostAsync<OAuthTokenResponse>("/v1/accounts/reset-password", new ResetPasswordRequest { Password = password }, bearer: token, cancellationToken: cancellationToken);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Value?.AccessToken))
            return ApiResult<string>.Fail(result.Error ?? "Could not reset the password.", result.FieldErrors, result.StatusCode);

        var handoff = await _session.EstablishAsync(result.Value, persist, returnUrl, cancellationToken);
        return ApiResult<string>.Ok(handoff);
    }

    public Task<ApiResult> ChangePasswordAsync(string currentPassword, string password, CancellationToken cancellationToken = default) =>
        _api.PostAsync("/v1/accounts/change-password", new ChangePasswordRequest
        {
            CurrentPassword = currentPassword,
            Password = password
        }, session: true, cancellationToken: cancellationToken);

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
