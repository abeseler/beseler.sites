using System.Net.Http.Json;
using BeselerNet.Shared.Contracts.OAuth;

namespace BeselerNet.Web.Shared;

internal sealed class TokenRefresher(IHttpClientFactory http, AuthCookie cookie, TimeProvider time)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<bool> EnsureAccessTokenAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        var ticket = cookie.Current;
        if (ticket is null)
            return false;

        if (!force && !ticket.AccessExpiresSoon(time))
            return true;

        if (string.IsNullOrWhiteSpace(ticket.RefreshToken))
        {
            cookie.Clear();
            return false;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            ticket = cookie.Current;
            if (ticket is null)
                return false;

            if (!force && !ticket.AccessExpiresSoon(time))
                return true;

            var client = http.CreateClient(ApiClient.ClientName);
            using var response = await client.PostAsJsonAsync("/v1/accounts/oauth/tokens", new OAuthTokenRequest
            {
                GrantType = OAuthGrantType.refresh_token,
                RefreshToken = ticket.RefreshToken
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                cookie.Clear();
                return false;
            }

            var tokens = await response.Content.ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken);
            if (string.IsNullOrWhiteSpace(tokens?.AccessToken))
            {
                cookie.Clear();
                return false;
            }

            cookie.Set(AuthTicket.From(tokens, time, ticket.Sid, ticket.Persist));
            return true;
        }
        catch (Exception)
        {
            return cookie.Current is not null && !cookie.Current.AccessExpiresSoon(time);
        }
        finally
        {
            _gate.Release();
        }
    }
}
