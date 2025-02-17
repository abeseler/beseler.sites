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

    /// <summary>
    /// This method will validate the token and return a claims principal if valid.
    /// </summary>
    public async Task<ClaimsPrincipal?> Validate(string token)
    {
        var result = await _handler.ValidateTokenAsync(token, _validationParameters);
        return result.IsValid ? new ClaimsPrincipal(result.ClaimsIdentity) : null;
    }

    /// <summary>
    /// Generate access and refresh tokens for the specified claims principal.
    /// </summary>
    public TokenResult Generate(ClaimsPrincipal principal)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var tokenId = Guid.CreateVersion7(utcNow);
        var expires = utcNow.AddSeconds(_expiresIn);
        var claims = principal.Claims.ToList();
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, tokenId.ToString()));
        var accessToken = WriteToken(principal.Claims, utcNow, expires);

        tokenId = Guid.CreateVersion7(utcNow);
        expires = utcNow.AddHours(_options.RefreshTokenLifetimeHours);
        claims.RemoveAll(x => x.Type != JwtRegisteredClaimNames.Sub);
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, tokenId.ToString()));

        var refreshToken = WriteToken(claims, utcNow, expires);

        return new()
        {
            AccessToken = accessToken,
            ExpiresIn = _expiresIn,
            RefreshToken = refreshToken,
            RefreshTokenId = tokenId,
            RefreshTokenExpires = expires
        };
    }

    /// <summary>
    /// Generate an access token with the specified subject and lifetime and optional additional claims. This method does not return a refresh token.
    /// <para>
    /// The <paramref name="subject"/> parameter must have a claim type of <see cref="JwtRegisteredClaimNames.Sub"/>.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public TokenResult Generate(Claim subject, TimeSpan lifetime, IEnumerable<Claim>? additionalClaims = null)
    {
        if (subject.Type != JwtRegisteredClaimNames.Sub)
        {
            throw new ArgumentException($"Claim type must be {JwtRegisteredClaimNames.Sub}", nameof(subject));
        }

        var utcNow = _timeProvider.GetUtcNow();
        var tokenId = Guid.CreateVersion7(utcNow);
        var expires = utcNow.Add(lifetime);
        var claims = new List<Claim>
        {
            subject,
            new(JwtRegisteredClaimNames.Jti, tokenId.ToString())
        };

        if (additionalClaims is not null)
        {
            claims.AddRange(additionalClaims);
        }           

        var token = WriteToken(claims, utcNow, expires);
        return new()
        {
            AccessToken = token,
            ExpiresIn = (long)lifetime.TotalSeconds
        };
    }

    private string WriteToken(IEnumerable<Claim> claims, DateTimeOffset issuedAt, DateTimeOffset expires)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new(claims),
            IssuedAt = issuedAt.DateTime,
            Expires = expires.DateTime,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = _signingCredentials
        };
        return _handler.CreateToken(descriptor);
    }
}

internal readonly struct TokenResult
{
    public string AccessToken { get; init; }
    public long ExpiresIn { get; init; }
    public string? RefreshToken { get; init; }
    public Guid? RefreshTokenId { get; init; }
    public DateTimeOffset? RefreshTokenExpires { get; init; }
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
