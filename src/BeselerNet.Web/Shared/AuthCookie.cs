using System.Text.Json;
using BeselerNet.Shared;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;

namespace BeselerNet.Web.Shared;

internal sealed class AuthCookie(IHttpContextAccessor http, IDataProtectionProvider protection, IMemoryCache cache)
{
    public const string CookieName = "beseler.session";
    private const string Purpose = "BeselerNet.Web.AuthCookie.v1";
    private const string SessionPrefix = "auth-session:";
    internal const string HandoffPrefix = "auth-handoff:";
    private static readonly TimeSpan Lifetime = AuthLimits.PersistCookie;

    private readonly IDataProtector _protector = protection.CreateProtector(Purpose);
    private readonly IHttpContextAccessor _http = http;
    private readonly IMemoryCache _cache = cache;
    private AuthTicket? _ticket;
    private bool _resolved;

    public AuthTicket? Current
    {
        get
        {
            if (!_resolved)
            {
                _ticket = Read();
                _resolved = true;
            }

            return _ticket;
        }
    }

    public void Set(AuthTicket ticket)
    {
        _ticket = ticket;
        _resolved = true;
        _cache.Set(SessionPrefix + ticket.Sid, ticket, Lifetime);
        WriteCookie(ticket);
    }

    public void Clear()
    {
        if (_ticket?.Sid is { } sid)
            _cache.Remove(SessionPrefix + sid);

        _ticket = null;
        _resolved = true;
        DeleteCookie();
    }

    public string CreateHandoff(AuthTicket ticket)
    {
        Set(ticket);
        var id = Guid.CreateVersion7().ToString("N");
        _cache.Set(HandoffPrefix + id, ticket, TimeSpan.FromMinutes(1));
        return id;
    }

    public AuthTicket? TakeHandoff(string id)
    {
        if (!_cache.TryGetValue(HandoffPrefix + id, out AuthTicket? ticket) || ticket is null)
            return null;

        _cache.Remove(HandoffPrefix + id);
        return ticket;
    }

    private AuthTicket? Read()
    {
        var value = _http.HttpContext?.Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            var json = _protector.Unprotect(value);
            var ticket = JsonSerializer.Deserialize<AuthTicket>(json, JsonSerializerOptions.Web);
            if (ticket is null)
                return null;

            return _cache.TryGetValue(SessionPrefix + ticket.Sid, out AuthTicket? cached) && cached is not null
                ? cached
                : ticket;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void WriteCookie(AuthTicket ticket)
    {
        if (_http.HttpContext is not { Response.HasStarted: false } context)
            return;

        var json = JsonSerializer.Serialize(ticket, JsonSerializerOptions.Web);
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true
        };
        if (ticket.Persist)
            options.Expires = DateTimeOffset.UtcNow.Add(Lifetime);
        else
            DeleteCookie();

        context.Response.Cookies.Append(CookieName, _protector.Protect(json), options);
    }

    private void DeleteCookie()
    {
        if (_http.HttpContext is not { Response.HasStarted: false } context)
            return;

        context.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }
}
