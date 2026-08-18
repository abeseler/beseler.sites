using BeselerNet.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace BeselerNet.Web.Shared;

internal sealed class SessionActivity(IMemoryCache cache, TimeProvider time)
{
    private readonly IMemoryCache _cache = cache;
    private readonly TimeProvider _time = time;

    public TimeSpan IdleAfter => AuthLimits.Idle;

    public void Touch(string sid) =>
        _cache.Set(Key(sid), _time.GetUtcNow(), TimeSpan.FromDays(1));

    public bool IsExpired(AuthTicket ticket)
    {
        if (ticket.Persist)
            return false;

        if (!_cache.TryGetValue(Key(ticket.Sid), out DateTimeOffset last))
            return false;

        return _time.GetUtcNow() - last > IdleAfter;
    }

    private static string Key(string sid) => "auth-activity:" + sid;
}
