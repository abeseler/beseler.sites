using System.Net;
using BeselerNet.Shared;

namespace BeselerNet.Web.Shared;

internal sealed record ApiResult(bool Succeeded, string? Error, IDictionary<string, string[]>? FieldErrors, HttpStatusCode? StatusCode, string? Title = null)
{
    public bool IsLocked => string.Equals(Title, AuthLimits.AccountLockedTitle, StringComparison.OrdinalIgnoreCase);

    public static ApiResult Ok(HttpStatusCode status = HttpStatusCode.OK) =>
        new(true, null, null, status);

    public static ApiResult Fail(string error, IDictionary<string, string[]>? fields = null, HttpStatusCode? status = null, string? title = null) =>
        new(false, error, fields, status, title);
}

internal sealed record ApiResult<T>(bool Succeeded, T? Value, string? Error, IDictionary<string, string[]>? FieldErrors, HttpStatusCode? StatusCode, string? Title = null)
{
    public bool IsLocked => string.Equals(Title, AuthLimits.AccountLockedTitle, StringComparison.OrdinalIgnoreCase);

    public static ApiResult<T> Ok(T? value, HttpStatusCode status = HttpStatusCode.OK) =>
        new(true, value, null, null, status);

    public static ApiResult<T> Fail(string error, IDictionary<string, string[]>? fields = null, HttpStatusCode? status = null, string? title = null) =>
        new(false, default, error, fields, status, title);

    public ApiResult WithoutValue() => new(Succeeded, Error, FieldErrors, StatusCode, Title);
}
