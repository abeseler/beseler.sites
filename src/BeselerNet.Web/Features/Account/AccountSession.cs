using System.Net;
using BeselerNet.Shared;
using BeselerNet.Shared.Contracts.OAuth;
using BeselerNet.Shared.Contracts.Users;
using BeselerNet.Web.Shared;

namespace BeselerNet.Web.Features.Account;

internal sealed class AccountSession(ApiClient api, AuthCookie cookie, TokenRefresher refresher, SessionActivity activity, TimeProvider time)
{
    private readonly ApiClient _api = api;
    private readonly AuthCookie _cookie = cookie;
    private readonly TokenRefresher _refresher = refresher;
    private readonly SessionActivity _activity = activity;
    private readonly TimeProvider _time = time;
    private bool _loaded;
    private string? _permissionToken;
    private AccessClaims _permissions = AccessClaims.Empty;

    public AccountProfileResponse? Profile { get; private set; }
    public bool HasSession { get; private set; }
    public bool Persist => _cookie.Current?.Persist is true;
    public string? Sid => _cookie.Current?.Sid;
    public string? RefreshToken => _cookie.Current?.RefreshToken;
    public bool EmailVerified => Profile?.EmailVerified is true;
    public string? Name => string.IsNullOrWhiteSpace(Profile?.Name) ? null : Profile.Name;

    public bool HasPermission(string resource, string action, string? scope = null)
    {
        var token = _cookie.Current?.AccessToken;
        if (!string.Equals(token, _permissionToken, StringComparison.Ordinal))
        {
            _permissionToken = token;
            _permissions = AccessClaims.FromAccessToken(token);
        }

        return _permissions.Has(resource, action, scope);
    }

    public bool SeesAdminNav =>
        HasPermission(Resources.Account, Actions.Read, Scopes.Global)
        || HasPermission(Resources.Role, Actions.Read)
        || HasPermission(Resources.Setting, Actions.Read);

    public bool HasRole(string name) => Profile?.HasRole(name) is true;

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded)
            return;

        var ticket = _cookie.Current;
        if (ticket is null || _activity.IsExpired(ticket))
        {
            if (ticket is not null)
                _cookie.Clear();
            ClearSession();
            _loaded = true;
            return;
        }

        if (!await _refresher.EnsureAccessTokenAsync(cancellationToken: cancellationToken))
        {
            ClearSession();
            _loaded = true;
            return;
        }

        _activity.Touch(ticket.Sid);
        HasSession = true;
        await RefreshProfileAsync(cancellationToken);
        _loaded = true;
    }

    public async Task<string> EstablishAsync(OAuthTokenResponse tokens, bool persist = false, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var ticket = AuthTicket.From(tokens, _time, persist: persist);
        var handoff = _cookie.CreateHandoff(ticket);
        _activity.Touch(ticket.Sid);
        HasSession = true;
        await RefreshProfileAsync(cancellationToken);
        _loaded = true;

        var dest = ReturnUrl.Destination(returnUrl, EmailVerified);
        return $"{Routes.EstablishSession}?ticket={handoff}&return={Uri.EscapeDataString(dest)}";
    }

    public void ReplaceTokens(OAuthTokenResponse tokens)
    {
        var current = _cookie.Current;
        if (current is null)
            return;

        _cookie.Set(AuthTicket.From(tokens, _time, current.Sid, current.Persist));
    }

    public async Task RefreshProfileAsync(CancellationToken cancellationToken = default)
    {
        var result = await _api.GetAsync<AccountProfileResponse>("/v1/accounts/me", session: true, cancellationToken);
        if (result.StatusCode is HttpStatusCode.Unauthorized)
        {
            _cookie.Clear();
            ClearSession();
            return;
        }

        if (result.Value is not null)
            Profile = result.Value;
    }

    private void ClearSession()
    {
        HasSession = false;
        Profile = null;
        _permissionToken = null;
        _permissions = AccessClaims.Empty;
    }
}
