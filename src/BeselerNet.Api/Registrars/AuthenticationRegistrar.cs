using BeselerNet.Api.Registrars;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace BeselerNet.Api.Registrars;

internal static class AuthenticationRegistrar
{
    public static void AddAuthentication(this IHostApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            var handler = new JsonWebTokenHandler();
            handler.InboundClaimTypeMap.Clear();
            options.TokenHandlers.Clear();
            options.TokenHandlers.Add(handler);

            var key = builder.Configuration.GetValue<string>("Jwt:Key") ?? throw new InvalidOperationException("Jwt:Key is required.");
            options.TokenValidationParameters = new()
            {
                ValidIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer"),
                ValidAudience = builder.Configuration.GetValue<string>("Jwt:Issuer"),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                NameClaimType = JwtRegisteredClaimNames.Sub,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        }).AddScheme<ApiKeyAuthOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthOptions.Scheme, null);

        builder.Services.AddAuthorizationBuilder()
            .AddDefaultPolicy("BearerOrApiKey", policy =>
            {
                _ = policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, ApiKeyAuthOptions.Scheme);
                _ = policy.RequireAuthenticatedUser();
            });
    }
}

internal sealed class ApiKeyAuthOptions : AuthenticationSchemeOptions
{
    public const string Scheme = "ApiKey";
    public const string HeaderName = "x-api-key";
}

internal sealed class ApiKeyAuthenticationHandler(
    HybridCache cache,
    IOptionsMonitor<ApiKeyAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<ApiKeyAuthOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthOptions.HeaderName, out var apiKeyHeaderValues)
            || apiKeyHeaderValues.FirstOrDefault() is not string providedApiKey)
        {
            return AuthenticateResult.NoResult();
        }

        var principal = await cache.GetOrCreateAsync(
            $"{ApiKeyAuthOptions.Scheme}:{providedApiKey}", async entry =>
            {
                await Task.CompletedTask;

                //TODO: get permissions and claims for the api key from the database
                //we need a mechanism to invalidate the cache when the api key is revoked or permissions are changed
                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, "0")
                };
                var identity = new ClaimsIdentity(claims, ApiKeyAuthOptions.Scheme);
                return new ClaimsPrincipal(identity);
            }, new HybridCacheEntryOptions()
            {
                LocalCacheExpiration = TimeSpan.FromMinutes(5),
                Expiration = TimeSpan.FromHours(1)
            });

        return principal is not null
            ? AuthenticateResult.Success(new AuthenticationTicket(principal, ApiKeyAuthOptions.Scheme))
            : AuthenticateResult.Fail("Invalid API Key.");
    }
}
