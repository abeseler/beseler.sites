using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace BeselerNet.Api.Accounts;

internal sealed class JwtGenerator
{
    private readonly TimeProvider _timeProvider;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;
    private readonly JsonWebTokenHandler _handler;
    private readonly JwtOptions _options;
    private readonly long _expiresIn;

    public JwtGenerator(TimeProvider timeProvider, IOptions<JwtOptions> options)
    {
        _timeProvider = timeProvider;
        _options = options.Value;
        _expiresIn = _options.AccessTokenLifetimeMinutes * 60;
        _handler = new();
        _handler.InboundClaimTypeMap.Clear();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Key!));
        _signingCredentials = new(key, SecurityAlgorithms.HmacSha256);
        _validationParameters = new()
        {
            IssuerSigningKey = key,
            ValidIssuer = options.Value.Issuer,
            ValidAudience = options.Value.Audience,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<ClaimsPrincipal?> Validate(string token)
    {
        var result = await _handler.ValidateTokenAsync(token, _validationParameters);
        return result.IsValid ? new ClaimsPrincipal(result.ClaimsIdentity) : null;
    }

    public TokenResult Generate(Account account)
    {
        var tokenId = Guid.NewGuid();
        var now = _timeProvider.GetUtcNow();
        var expires = now.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var claims = GetDefaultClaims(account, tokenId);

        //TODO: add access token claims

        var accessToken = WriteToken(claims, expires);

        tokenId = Guid.CreateVersion7(now);
        expires = now.AddHours(_options.RefreshTokenLifetimeHours);
        claims = GetDefaultClaims(account, tokenId);

        var refreshToken = WriteToken(claims, expires);

        return new()
        {
            Token = accessToken,
            ExpiresIn = _expiresIn,
            RefreshToken = refreshToken,
            RefreshTokenId = tokenId
        };
    }

    public TokenResult Generate(Account account, TimeSpan lifetime, IEnumerable<Claim>? additionalClaims = null)
    {
        var tokenId = Guid.NewGuid();
        var expires = _timeProvider.GetUtcNow().Add(lifetime);
        var claims = GetDefaultClaims(account, tokenId);

        if (additionalClaims is not null)
        {
            claims.AddRange(additionalClaims);
        }           

        var token = WriteToken(claims, expires);
        return new()
        {
            Token = token,
            ExpiresIn = (long)lifetime.TotalSeconds
        };
    }

    private string WriteToken(List<Claim> claims, DateTimeOffset expires)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new(claims),
            Expires = expires.DateTime,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = _signingCredentials
        };
        return _handler.CreateToken(descriptor);
    }

    private static List<Claim> GetDefaultClaims(Account account, Guid tokenId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, tokenId.ToString()),
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString())
        };
        if (account.Email is not null)
        {
            claims.Add(new(JwtRegisteredClaimNames.Email, account.Email));
        }
        return claims;
    }
}

internal readonly struct TokenResult
{
    public string Token { get; init; }
    public long ExpiresIn { get; init; }
    public string? RefreshToken { get; init; }
    public Guid? RefreshTokenId { get; init; }
}

internal sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string Key { get; init; }
    public int AccessTokenLifetimeMinutes { get; init; }
    public int RefreshTokenLifetimeHours { get; init; }
}
