using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BeselerNet.Shared.Contracts.OAuth;
using BeselerNet.Shared.Contracts.Users;

namespace BeselerNet.Web.Services;

internal sealed class AuthService(IHttpClientFactory httpFactory, LocalStorageAccessor storage)
{
    public const string AccessTokenKey = "access_token";
    private const string EmailVerifiedKey = "email_verified";
    private const string ProfileKey = "account_profile";
    private const string ClientId = "beseler-net-web";

    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly LocalStorageAccessor _storage = storage;
    private AccountProfileResponse? _profile;

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var client = _httpFactory.CreateClient("beseler-net-api");
        var request = new OAuthTokenRequest
        {
            GrantType = OAuthGrantType.password,
            Username = email,
            Password = password,
            ClientId = ClientId
        };

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/v1/accounts/oauth/tokens", request, cancellationToken);
        }
        catch (Exception)
        {
            return AuthResult.Fail("Could not reach the API. Is it running?");
        }

        if (response.IsSuccessStatusCode)
        {
            var tokens = await response.Content.ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken);
            if (tokens is null)
                return AuthResult.Fail("The API returned an empty token response.");

            await _storage.SetItemAsync(AccessTokenKey, tokens.AccessToken);
            await _storage.SetItemAsync(EmailVerifiedKey, JwtHasVerifiedEmail(tokens.AccessToken));
            await RefreshProfileAsync(cancellationToken);

            return AuthResult.Ok();
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized)
            return AuthResult.Fail("Email or password is wrong.");

        return await ReadProblem(response, cancellationToken);
    }

    public async Task<AuthResult> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var client = _httpFactory.CreateClient("beseler-net-api");
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/v1/accounts/register-user", request, cancellationToken);
        }
        catch (Exception)
        {
            return AuthResult.Fail("Could not reach the API. Is it running?");
        }

        if (response.StatusCode is HttpStatusCode.Created)
            return await LoginAsync(request.Email, request.Password, cancellationToken);

        return await ReadProblem(response, cancellationToken);
    }

    public async Task<bool> IsEmailVerified()
    {
        var profile = await GetProfileAsync();
        if (profile is not null)
            return profile.EmailVerified;

        var token = await _storage.GetItemAsync<string>(AccessTokenKey);
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (JwtHasVerifiedEmail(token))
            return true;

        return await _storage.GetItemAsync<bool>(EmailVerifiedKey);
    }

    public async Task MarkEmailVerified()
    {
        await _storage.SetItemAsync(EmailVerifiedKey, true);
        var profile = _profile ?? await _storage.GetItemAsync<AccountProfileResponse>(ProfileKey);
        if (profile is not null)
            await StoreProfile(profile with { EmailVerified = true });
    }

    public async Task<bool> HasSession()
    {
        var token = await _storage.GetItemAsync<string>(AccessTokenKey);
        return !string.IsNullOrWhiteSpace(token);
    }

    public async Task<AccountProfileResponse?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        if (_profile is not null)
            return _profile;

        _profile = await _storage.GetItemAsync<AccountProfileResponse>(ProfileKey);
        if (_profile is not null)
            return _profile;

        return await RefreshProfileAsync(cancellationToken);
    }

    public async Task<bool> HasPermission(string resource, string action, string? scope = null)
    {
        var profile = await GetProfileAsync();
        return profile?.HasPermission(resource, action, scope) is true;
    }

    public async Task<bool> HasRole(string name)
    {
        var profile = await GetProfileAsync();
        return profile?.HasRole(name) is true;
    }

    public async Task<string?> GetSignedInName()
    {
        var profile = await GetProfileAsync();
        if (!string.IsNullOrWhiteSpace(profile?.Name))
            return profile.Name;

        var token = await _storage.GetItemAsync<string>(AccessTokenKey);
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var payload = ReadJwtPayload(token);
        if (payload is null)
            return null;

        if (payload.Value.TryGetProperty("name", out var name) && name.ValueKind is JsonValueKind.String)
            return name.GetString();

        return null;
    }

    public async Task SignOut()
    {
        _profile = null;
        await _storage.RemoveItemAsync(AccessTokenKey);
        await _storage.RemoveItemAsync(EmailVerifiedKey);
        await _storage.RemoveItemAsync(ProfileKey);
    }

    public async Task<AuthResult> ConfirmEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        var client = _httpFactory.CreateClient("beseler-net-api");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/accounts/confirm-email");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (Exception)
        {
            return AuthResult.Fail("Could not reach the API. Is it running?");
        }

        if (response.StatusCode is HttpStatusCode.NoContent)
            return AuthResult.Ok();

        if (response.StatusCode is HttpStatusCode.Unauthorized)
            return AuthResult.Fail("This confirmation link is invalid or has expired.");

        return await ReadProblem(response, cancellationToken);
    }

    public async Task<AuthResult> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var client = _httpFactory.CreateClient("beseler-net-api");
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/v1/accounts/forgot-password", new ForgotPasswordRequest { Email = email }, cancellationToken);
        }
        catch (Exception)
        {
            return AuthResult.Fail("Could not reach the API. Is it running?");
        }

        if (response.StatusCode is HttpStatusCode.Accepted)
            return AuthResult.Ok();

        return await ReadProblem(response, cancellationToken);
    }

    public async Task<AuthResult> ResetPasswordAsync(string token, string password, CancellationToken cancellationToken = default)
    {
        var client = _httpFactory.CreateClient("beseler-net-api");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/accounts/reset-password");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new ResetPasswordRequest { Password = password });

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (Exception)
        {
            return AuthResult.Fail("Could not reach the API. Is it running?");
        }

        if (response.StatusCode is HttpStatusCode.NoContent)
            return AuthResult.Ok();

        if (response.StatusCode is HttpStatusCode.Unauthorized)
            return AuthResult.Fail("This reset link is invalid or has expired.");

        return await ReadProblem(response, cancellationToken);
    }

    public async Task<AuthResult> ResendVerificationAsync(CancellationToken cancellationToken = default)
    {
        var token = await _storage.GetItemAsync<string>(AccessTokenKey);
        if (string.IsNullOrWhiteSpace(token))
            return AuthResult.Fail("Sign in first so we know which address to write to.");

        var client = _httpFactory.CreateClient("beseler-net-api");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/accounts/resend-email-confirmation");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (Exception)
        {
            return AuthResult.Fail("Could not reach the API. Is it running?");
        }

        if (response.StatusCode is HttpStatusCode.NoContent)
            return AuthResult.Ok();

        if (response.StatusCode is HttpStatusCode.Unauthorized)
            return AuthResult.Fail("Your session expired. Sign in again, then resend.");

        return await ReadProblem(response, cancellationToken);
    }

    public async Task<AccountProfileResponse?> RefreshProfileAsync(CancellationToken cancellationToken = default)
    {
        var token = await _storage.GetItemAsync<string>(AccessTokenKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            await ClearProfile();
            return null;
        }

        var client = _httpFactory.CreateClient("beseler-net-api");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/accounts/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (Exception)
        {
            return _profile ?? await _storage.GetItemAsync<AccountProfileResponse>(ProfileKey);
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized)
        {
            await SignOut();
            return null;
        }

        if (!response.IsSuccessStatusCode)
            return _profile ?? await _storage.GetItemAsync<AccountProfileResponse>(ProfileKey);

        var profile = await response.Content.ReadFromJsonAsync<AccountProfileResponse>(cancellationToken);
        if (profile is not null)
            await StoreProfile(profile);

        return profile;
    }

    private async Task StoreProfile(AccountProfileResponse profile)
    {
        _profile = profile;
        await _storage.SetItemAsync(ProfileKey, profile);
        await _storage.SetItemAsync(EmailVerifiedKey, profile.EmailVerified);
    }

    private async Task ClearProfile()
    {
        _profile = null;
        await _storage.RemoveItemAsync(ProfileKey);
    }

    private static bool JwtHasVerifiedEmail(string token)
    {
        var payload = ReadJwtPayload(token);
        if (payload is null || !payload.Value.TryGetProperty("email_verified", out var verified))
            return false;

        return verified.ValueKind is JsonValueKind.True
            || string.Equals(verified.GetString(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static JsonElement? ReadJwtPayload(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
            return null;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            return JsonDocument.Parse(json).RootElement.Clone();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<AuthResult> ReadProblem(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
            if (problem?.Errors is { Count: > 0 })
                return AuthResult.Fail(problem.Title ?? "Check the highlighted fields.", problem.Errors);

            if (!string.IsNullOrWhiteSpace(problem?.Detail))
                return AuthResult.Fail(problem.Detail);
        }
        catch (JsonException)
        {
        }

        return AuthResult.Fail($"Something went wrong ({(int)response.StatusCode}).");
    }
}

internal sealed record AuthResult(bool Succeeded, string? Error, IDictionary<string, string[]>? FieldErrors)
{
    public static AuthResult Ok() => new(true, null, null);
    public static AuthResult Fail(string error, IDictionary<string, string[]>? fields = null) => new(false, error, fields);
}
