using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Infrastructure.Security;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests.Security;

public class JwtValidationTests
{
    private readonly SynaxisConfiguration _config;

    public JwtValidationTests()
    {
        _config = new SynaxisConfiguration
        {
            JwtSecret = "THIS_IS_A_VERY_LONG_SECRET_KEY_FOR_TESTING_PURPOSES_ONLY_1234567890",
            JwtIssuer = "TestIssuer",
            JwtAudience = "TestAudience"
        };
    }

    [Fact]
    public void ValidateToken_WithValidToken_Succeeds()
    {
        var jwtService = new JwtService(Options.Create(_config));
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Email = "test@example.com",
            Role = UserRole.Developer
        };

        var token = jwtService.GenerateToken(user);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config.JwtSecret!);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _config.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = _config.JwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

        Assert.NotNull(principal.Identity);
        Assert.True(principal.Identity.IsAuthenticated);
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ThrowsSecurityTokenExpiredException()
    {
        var expiredToken = GenerateExpiredToken(_config);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config.JwtSecret!);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _config.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = _config.JwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        Assert.Throws<SecurityTokenExpiredException>(() => tokenHandler.ValidateToken(expiredToken, validationParameters, out _));
    }

    [Fact]
    public void ValidateToken_WithWrongSigningKey_ThrowsSecurityTokenSignatureKeyNotFoundException()
    {
        var wrongConfig = new SynaxisConfiguration
        {
            JwtSecret = "DIFFERENT_SECRET_KEY_FOR_TESTING_1234567890",
            JwtIssuer = "TestIssuer",
            JwtAudience = "TestAudience"
        };

        var jwtService = new JwtService(Options.Create(wrongConfig));
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Email = "test@example.com",
            Role = UserRole.Developer
        };

        var token = jwtService.GenerateToken(user);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config.JwtSecret!);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _config.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = _config.JwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        Assert.ThrowsAny<SecurityTokenValidationException>(() => tokenHandler.ValidateToken(token, validationParameters, out _));
    }

    [Fact]
    public void ValidateToken_WithInvalidIssuer_ThrowsSecurityTokenInvalidIssuerException()
    {
        var token = GenerateTokenWithInvalidIssuer(_config);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config.JwtSecret!);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _config.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = _config.JwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        Assert.Throws<SecurityTokenInvalidIssuerException>(() => tokenHandler.ValidateToken(token, validationParameters, out _));
    }

    [Fact]
    public void ValidateToken_WithInvalidAudience_ThrowsSecurityTokenInvalidAudienceException()
    {
        var token = GenerateTokenWithInvalidAudience(_config);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config.JwtSecret!);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _config.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = _config.JwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        Assert.Throws<SecurityTokenInvalidAudienceException>(() => tokenHandler.ValidateToken(token, validationParameters, out _));
    }

    [Fact]
    public void ValidateToken_WithMalformedToken_ThrowsArgumentException()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config.JwtSecret!);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _config.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = _config.JwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        Assert.ThrowsAny<ArgumentException>(() => tokenHandler.ValidateToken("not.a.valid.jwt.token", validationParameters, out _));
    }

    private string GenerateExpiredToken(SynaxisConfiguration config)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(config.JwtSecret!);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
            new Claim("role", UserRole.Developer.ToString()),
            new Claim("tenantId", Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(-1),
            Issuer = config.JwtIssuer,
            Audience = config.JwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateTokenWithInvalidIssuer(SynaxisConfiguration config)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(config.JwtSecret!);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
            new Claim("role", UserRole.Developer.ToString()),
            new Claim("tenantId", Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            Issuer = "WrongIssuer",
            Audience = config.JwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateTokenWithInvalidAudience(SynaxisConfiguration config)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(config.JwtSecret!);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
            new Claim("role", UserRole.Developer.ToString()),
            new Claim("tenantId", Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            Issuer = config.JwtIssuer,
            Audience = "WrongAudience",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
