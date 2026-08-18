using System.Net;
using BeselerNet.Shared.Contracts.OAuth;
using BeselerNet.Shared.Contracts.Users;
using BeselerNet.Web.Shared;

namespace BeselerNet.Web.Features.Account;

internal sealed class AccountSession(ApiClient api, AuthCookie cookie, TokenRefresher refresher, TimeProvider time)
{
    private readonly ApiClient _api = api;
    private readonly AuthCookie _cookie = cookie;
    private readonly TokenRefresher _refresher = refresher;
    private readonly TimeProvider _time = time;
    private bool _loaded;

    public AccountProfileResponse? Profile { get; private set; }
    public bool HasSession { get; private set; }
    public bool Persist => _cookie.Current?.Persist is true;
    public bool EmailVerified => Profile?.EmailVerified is true;
    public string? Name => string.IsNullOrWhiteSpace(Profile?.Name) ? null : Profile.Name;

    public bool HasPermission(string resource, string action, string? scope = null) =>
        Profile?.HasPermission(resource, action, scope) is true;

    public bool HasRole(string name) => Profile?.HasRole(name) is true;

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded)
            return;

        if (_cookie.Current is null || !await _refresher.EnsureAccessTokenAsync(cancellationToken: cancellationToken))
        {
            HasSession = false;
            Profile = null;
            _loaded = true;
            return;
        }

        HasSession = true;
        await RefreshProfileAsync(cancellationToken);
        _loaded = true;
    }

    public async Task<string> EstablishAsync(OAuthTokenResponse tokens, bool persist = false, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var ticket = AuthTicket.From(tokens, _time, persist: persist);
        var handoff = _cookie.CreateHandoff(ticket);
        HasSession = true;
        await RefreshProfileAsync(cancellationToken);
        _loaded = true;

        var dest = ReturnUrl.Destination(returnUrl, EmailVerified);
        return $"{Routes.EstablishSession}?ticket={handoff}&return={Uri.EscapeDataString(dest)}";
    }

    public async Task RefreshProfileAsync(CancellationToken cancellationToken = default)
    {
        var result = await _api.GetAsync<AccountProfileResponse>("/v1/accounts/me", session: true, cancellationToken);
        if (result.StatusCode is HttpStatusCode.Unauthorized)
        {
            HasSession = false;
            Profile = null;
            _cookie.Clear();
            return;
        }

        if (result.Value is not null)
            Profile = result.Value;
    }
}
