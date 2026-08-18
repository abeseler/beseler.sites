using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BeselerNet.Web.Shared;

internal sealed class ApiClient(IHttpClientFactory httpFactory, AuthCookie cookie, TokenRefresher refresher)
{
    public const string ClientName = "beseler-net-api";

    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly AuthCookie _cookie = cookie;
    private readonly TokenRefresher _refresher = refresher;

    public Task<ApiResult> PostAsync(string path, object? body = null, string? bearer = null, bool session = false, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, path, body, bearer, session, cancellationToken);

    public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, string? bearer = null, bool session = false, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Post, path, body, bearer, session, retry: false, cancellationToken);

    public Task<ApiResult<T>> GetAsync<T>(string path, bool session = false, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, path, bearer: null, session: session, cancellationToken: cancellationToken);

    public async Task<ApiResult> SendAsync(
        HttpMethod method,
        string path,
        object? body = null,
        string? bearer = null,
        bool session = false,
        CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<object>(method, path, body, bearer, session, retry: false, cancellationToken);
        return result.WithoutValue();
    }

    public async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body = null,
        string? bearer = null,
        bool session = false,
        bool retry = false,
        CancellationToken cancellationToken = default)
    {
        var client = _httpFactory.CreateClient(ClientName);
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body);

        var token = bearer;
        if (session)
            token ??= _cookie.Current?.AccessToken;

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (Exception)
        {
            return ApiResult<T>.Fail("Could not reach the API. Is it running?");
        }

        if (session && !retry && response.StatusCode is HttpStatusCode.Unauthorized
            && await _refresher.EnsureAccessTokenAsync(force: true, cancellationToken))
        {
            response.Dispose();
            return await SendAsync<T>(method, path, body, bearer: null, session: true, retry: true, cancellationToken);
        }

        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.Accepted
                || response.Content.Headers.ContentLength is 0)
            {
                return ApiResult<T>.Ok(default, response.StatusCode);
            }

            try
            {
                var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
                return ApiResult<T>.Ok(value, response.StatusCode);
            }
            catch (JsonException)
            {
                return ApiResult<T>.Ok(default, response.StatusCode);
            }
        }

        return await ReadProblem<T>(response, cancellationToken);
    }

    private static async Task<ApiResult<T>> ReadProblem<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
            if (problem?.Errors is { Count: > 0 })
                return ApiResult<T>.Fail(problem.Title ?? "Check the highlighted fields.", problem.Errors, response.StatusCode);

            if (!string.IsNullOrWhiteSpace(problem?.Detail))
                return ApiResult<T>.Fail(problem.Detail, status: response.StatusCode);
        }
        catch (JsonException)
        {
        }

        return ApiResult<T>.Fail($"Something went wrong ({(int)response.StatusCode}).", status: response.StatusCode);
    }
}
