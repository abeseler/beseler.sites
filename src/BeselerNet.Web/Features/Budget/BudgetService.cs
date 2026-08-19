using BeselerNet.Shared.Contracts.Budget;
using BeselerNet.Web.Shared;

namespace BeselerNet.Web.Features.Budget;

internal sealed class BudgetService(ApiClient api)
{
    private readonly ApiClient _api = api;

    public Task<ApiResult<BudgetYearsResponse>> ListYearsAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<BudgetYearsResponse>("/v1/budget/years", session: true, cancellationToken);

    public Task<ApiResult<BudgetYearResponse>> GetYearAsync(int year, CancellationToken cancellationToken = default) =>
        _api.GetAsync<BudgetYearResponse>($"/v1/budget/years/{year}", session: true, cancellationToken);

    public Task<ApiResult<BudgetYearResponse>> StartYearAsync(int year, decimal startingBalance, CancellationToken cancellationToken = default) =>
        _api.PostAsync<BudgetYearResponse>($"/v1/budget/years/{year}", new StartBudgetYearRequest { StartingBalance = startingBalance }, session: true, cancellationToken: cancellationToken);

    public Task<ApiResult<BudgetYearResponse>> SetStartingBalanceAsync(int year, decimal startingBalance, CancellationToken cancellationToken = default) =>
        _api.PutAsync<BudgetYearResponse>($"/v1/budget/years/{year}/starting-balance", new SetStartingBalanceRequest { StartingBalance = startingBalance }, session: true, cancellationToken);

    public Task<ApiResult> DeleteYearAsync(int year, CancellationToken cancellationToken = default) =>
        _api.DeleteAsync($"/v1/budget/years/{year}", session: true, cancellationToken);

    public Task<ApiResult<BudgetPack>> ExportYearAsync(int year, CancellationToken cancellationToken = default) =>
        _api.GetAsync<BudgetPack>($"/v1/budget/years/{year}/export", session: true, cancellationToken);

    public Task<ApiResult<BudgetYearResponse>> ImportYearAsync(int year, BudgetPack pack, CancellationToken cancellationToken = default) =>
        _api.PostAsync<BudgetYearResponse>($"/v1/budget/years/{year}/import", pack, session: true, cancellationToken: cancellationToken);

    public Task<ApiResult<BudgetMonthResponse>> GetMonthAsync(int year, int month, CancellationToken cancellationToken = default) =>
        _api.GetAsync<BudgetMonthResponse>($"/v1/budget/years/{year}/months/{month}", session: true, cancellationToken);

    public Task<ApiResult<BudgetLineResponse>> CreateLineAsync(int year, int month, UpsertBudgetLineRequest request, CancellationToken cancellationToken = default) =>
        _api.PostAsync<BudgetLineResponse>($"/v1/budget/years/{year}/months/{month}/lines", request, session: true, cancellationToken: cancellationToken);

    public Task<ApiResult<BudgetLineResponse>> UpdateLineAsync(int lineId, UpsertBudgetLineRequest request, CancellationToken cancellationToken = default) =>
        _api.PutAsync<BudgetLineResponse>($"/v1/budget/lines/{lineId}", request, session: true, cancellationToken);

    public Task<ApiResult> DeleteLineAsync(int lineId, CancellationToken cancellationToken = default) =>
        _api.DeleteAsync($"/v1/budget/lines/{lineId}", session: true, cancellationToken);

    public Task<ApiResult<IReadOnlyList<BudgetTemplateResponse>>> ListTemplatesAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<IReadOnlyList<BudgetTemplateResponse>>("/v1/budget/templates", session: true, cancellationToken);

    public Task<ApiResult<BudgetTemplateResponse>> CreateTemplateAsync(UpsertBudgetTemplateRequest request, CancellationToken cancellationToken = default) =>
        _api.PostAsync<BudgetTemplateResponse>("/v1/budget/templates", request, session: true, cancellationToken: cancellationToken);

    public Task<ApiResult<BudgetTemplateResponse>> UpdateTemplateAsync(int templateId, UpsertBudgetTemplateRequest request, CancellationToken cancellationToken = default) =>
        _api.PutAsync<BudgetTemplateResponse>($"/v1/budget/templates/{templateId}", request, session: true, cancellationToken);

    public Task<ApiResult> DeleteTemplateAsync(int templateId, CancellationToken cancellationToken = default) =>
        _api.DeleteAsync($"/v1/budget/templates/{templateId}", session: true, cancellationToken);
}
