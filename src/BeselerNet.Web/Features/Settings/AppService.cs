using BeselerNet.Shared.Contracts.App;
using BeselerNet.Web.Shared;

namespace BeselerNet.Web.Features.Settings;

internal sealed class AppService(ApiClient api)
{
    private readonly ApiClient _api = api;

    public Task<ApiResult<AppStatusResponse>> GetStatusAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<AppStatusResponse>("/v1/app", session: false, cancellationToken);

    public Task<ApiResult<IReadOnlyList<AppSettingResponse>>> ListAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<IReadOnlyList<AppSettingResponse>>("/v1/settings", session: true, cancellationToken);

    public Task<ApiResult<AppSettingResponse>> UpdateAsync(string key, string value, CancellationToken cancellationToken = default) =>
        _api.PutAsync<AppSettingResponse>($"/v1/settings/{key}", new UpdateSettingRequest { Value = value }, session: true, cancellationToken);
}
