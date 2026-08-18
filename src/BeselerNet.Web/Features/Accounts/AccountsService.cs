using BeselerNet.Shared.Contracts.Users;
using BeselerNet.Web.Shared;

namespace BeselerNet.Web.Features.Accounts;

internal sealed class AccountsService(ApiClient api)
{
    private readonly ApiClient _api = api;

    public Task<ApiResult<IReadOnlyList<AccountResponse>>> ListAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<IReadOnlyList<AccountResponse>>("/v1/accounts", session: true, cancellationToken);

    public Task<ApiResult<AccountResponse>> GetAsync(int accountId, CancellationToken cancellationToken = default) =>
        _api.GetAsync<AccountResponse>($"/v1/accounts/{accountId}", session: true, cancellationToken);

    public Task<ApiResult<AccountResponse>> UpdateAsync(int accountId, string givenName, string familyName, CancellationToken cancellationToken = default) =>
        _api.PutAsync<AccountResponse>($"/v1/accounts/{accountId}", new UpdateAccountRequest
        {
            GivenName = givenName,
            FamilyName = familyName
        }, session: true, cancellationToken);

    public Task<ApiResult<AccountResponse>> DisableAsync(int accountId, CancellationToken cancellationToken = default) =>
        _api.PostAsync<AccountResponse>($"/v1/accounts/{accountId}/disable", session: true, cancellationToken: cancellationToken);

    public Task<ApiResult<AccountResponse>> EnableAsync(int accountId, CancellationToken cancellationToken = default) =>
        _api.PostAsync<AccountResponse>($"/v1/accounts/{accountId}/enable", session: true, cancellationToken: cancellationToken);

    public Task<ApiResult<AccountResponse>> UnlockAsync(int accountId, CancellationToken cancellationToken = default) =>
        _api.PostAsync<AccountResponse>($"/v1/accounts/{accountId}/unlock", session: true, cancellationToken: cancellationToken);

    public Task<ApiResult<AccountResponse>> SetRolesAsync(int accountId, IReadOnlyList<AccountRoleAssignment> roles, CancellationToken cancellationToken = default) =>
        _api.PutAsync<AccountResponse>($"/v1/accounts/{accountId}/roles", new SetAccountRolesRequest { Roles = roles }, session: true, cancellationToken);

    public Task<ApiResult<AccountResponse>> InviteAsync(InviteUserRequest request, CancellationToken cancellationToken = default) =>
        _api.PostAsync<AccountResponse>("/v1/accounts/invite", request, session: true, cancellationToken: cancellationToken);

    public Task<ApiResult> ResendInviteAsync(int accountId, CancellationToken cancellationToken = default) =>
        _api.PostAsync($"/v1/accounts/{accountId}/resend-invite", session: true, cancellationToken: cancellationToken);
}
