using System.Net;
using BeselerNet.Shared.Contracts.Users;
using BeselerNet.Web.Shared;

namespace BeselerNet.Web.Features.Account;

internal sealed class AccountSession(ApiClient api, LocalStorageAccessor storage)
{
    private const string ProfileKey = "account_profile";

    private readonly ApiClient _api = api;
    private readonly LocalStorageAccessor _storage = storage;
    private bool _loaded;

    public AccountProfileResponse? Profile { get; private set; }
    public bool HasSession { get; private set; }
    public bool EmailVerified => Profile?.EmailVerified is true;
    public string? Name => string.IsNullOrWhiteSpace(Profile?.Name) ? null : Profile.Name;

    public bool HasPermission(string resource, string action, string? scope = null) =>
        Profile?.HasPermission(resource, action, scope) is true;

    public bool HasRole(string name) => Profile?.HasRole(name) is true;

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded)
            return;

        var token = await _storage.TryGetItemAsync<string>(ApiClient.AccessTokenKey);
        if (!token.Available)
            return;

        HasSession = !string.IsNullOrWhiteSpace(token.Value);
        if (HasSession)
        {
            var cached = await _storage.TryGetItemAsync<AccountProfileResponse>(ProfileKey);
            if (cached.Available)
                Profile = cached.Value;
            if (Profile is null)
                await RefreshProfileAsync(cancellationToken);
        }

        _loaded = true;
    }

    public async Task StartAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        await _storage.SetItemAsync(ApiClient.AccessTokenKey, accessToken);
        HasSession = true;
        await RefreshProfileAsync(cancellationToken);
        _loaded = true;
    }

    public async Task RefreshProfileAsync(CancellationToken cancellationToken = default)
    {
        var result = await _api.GetAsync<AccountProfileResponse>("/v1/accounts/me", session: true, cancellationToken);
        if (result.StatusCode is HttpStatusCode.Unauthorized)
        {
            await SignOutAsync();
            return;
        }

        if (result.Value is not null)
            await StoreProfile(result.Value);
    }

    public async Task SignOutAsync()
    {
        Profile = null;
        HasSession = false;
        _loaded = true;
        await _storage.RemoveItemAsync(ApiClient.AccessTokenKey);
        await _storage.RemoveItemAsync(ProfileKey);
    }

    private async Task StoreProfile(AccountProfileResponse profile)
    {
        Profile = profile;
        await _storage.SetItemAsync(ProfileKey, profile);
    }
}
