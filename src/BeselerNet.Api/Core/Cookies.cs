namespace BeselerNet.Api.Core;

internal sealed class Cookies(IHttpContextAccessor accessor)
{
    private static readonly CookieOptions DefaultOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict
    };
    private readonly HttpContext? _context = accessor.HttpContext;
    public string? Get(string key) =>
        _context?.Request.Cookies.TryGetValue(key, out var value) ?? false ? value : null;
    public void Set(string key, string value, CookieOptions? options = null) =>
        _context?.Response.Cookies.Append(key, value, options ?? DefaultOptions);
    public void Delete(string key) =>
        _context?.Response.Cookies.Delete(key);
}
