// <copyright file="AuthenticationService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for authentication operations including JWT token generation and validation.
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly SynaxisDbContext _context;
        private readonly IUserService _userService;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            SynaxisDbContext context,
            IUserService userService,
            IOptions<JwtOptions> jwtOptions,
            ILogger<AuthenticationService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AuthenticationResult> AuthenticateAsync(string email, string password)
        {
            try
            {
                _logger.LogInformation("Authentication attempt for email: {Email}", email);

                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Authentication failed: Email is required");
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Email is required"
                    };
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Authentication failed: Password is required");
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Password is required"
                    };
                }

                // Authenticate user
                var user = await _userService.AuthenticateAsync(email, password);

                // Check if MFA is enabled
                if (user.MfaEnabled)
                {
                    _logger.LogInformation("MFA required for user: {UserId}", user.Id);
                    return new AuthenticationResult
                    {
                        Success = false,
                        RequiresMfa = true,
                        User = user,
                        ErrorMessage = "MFA code required"
                    };
                }

                // Generate tokens
                var accessToken = GenerateAccessToken(user);
                var refreshToken = await GenerateRefreshTokenAsync(user.Id);

                _logger.LogInformation("Authentication successful for user: {UserId}", user.Id);

                return new AuthenticationResult
                {
                    Success = true,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = _jwtOptions.AccessTokenExpirationMinutes * 60,
                    User = user,
                    RequiresMfa = false
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Authentication failed for email: {Email}", email);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication for email: {Email}", email);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred during authentication"
                };
            }
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("Token refresh attempt");

                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    _logger.LogWarning("Token refresh failed: Refresh token is required");
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Refresh token is required"
                    };
                }

                // Find the refresh token
                var tokenHash = HashToken(refreshToken);
                var refreshTokenEntity = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

                if (refreshTokenEntity == null)
                {
                    _logger.LogWarning("Token refresh failed: Refresh token not found");
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid refresh token"
                    };
                }

                if (refreshTokenEntity.IsRevoked)
                {
                    _logger.LogWarning("Token refresh failed: Refresh token has been revoked");
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Refresh token has been revoked"
                    };
                }

                if (refreshTokenEntity.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning("Token refresh failed: Refresh token has expired");
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Refresh token has expired"
                    };
                }

                if (!refreshTokenEntity.User.IsActive)
                {
                    _logger.LogWarning("Token refresh failed: User account is not active");
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "User account is not active"
                    };
                }

                // Generate new tokens first
                var accessToken = GenerateAccessToken(refreshTokenEntity.User);
                var newRefreshToken = await GenerateRefreshTokenAsync(refreshTokenEntity.User.Id);

                // Revoke old refresh token and set replacement tracking
                refreshTokenEntity.IsRevoked = true;
                refreshTokenEntity.RevokedAt = DateTime.UtcNow;
                refreshTokenEntity.ReplacedByTokenHash = newRefreshToken;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Token refresh successful for user: {UserId}", refreshTokenEntity.User.Id);

                return new AuthenticationResult
                {
                    Success = true,
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresIn = _jwtOptions.AccessTokenExpirationMinutes * 60,
                    User = refreshTokenEntity.User
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred during token refresh"
                };
            }
        }

        public async Task LogoutAsync(string refreshToken, string accessToken = null)
        {
            try
            {
                _logger.LogInformation("Logout attempt");

                // Invalidate JWT access token if provided
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    var tokenId = GetTokenIdFromJwt(accessToken);
                    if (tokenId != null)
                    {
                        var userId = GetUserIdFromToken(accessToken);
                        if (userId.HasValue)
                        {
                            var expiresAt = GetTokenExpirationFromJwt(accessToken);

                            var jwtBlacklist = new JwtBlacklist
                            {
                                Id = Guid.NewGuid(),
                                UserId = userId.Value,
                                TokenId = tokenId,
                                CreatedAt = DateTime.UtcNow,
                                ExpiresAt = expiresAt
                            };

                            _context.JwtBlacklists.Add(jwtBlacklist);
                            _logger.LogInformation("JWT access token added to blacklist: {TokenId}", tokenId);
                        }
                    }
                }

                // Revoke refresh token if provided
                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    var tokenHash = HashToken(refreshToken);
                    var refreshTokenEntity = await _context.RefreshTokens
                        .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

                    if (refreshTokenEntity != null)
                    {
                        refreshTokenEntity.IsRevoked = true;
                        refreshTokenEntity.RevokedAt = DateTime.UtcNow;
                        _logger.LogInformation("Refresh token revoked for user: {UserId}", refreshTokenEntity.UserId);
                    }
                    else
                    {
                        _logger.LogWarning("Logout: Refresh token not found");
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Logout successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
            }
        }

        public bool ValidateToken(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return false;
                }

                // Check if token is blacklisted
                var tokenId = GetTokenIdFromJwt(token);
                if (tokenId != null)
                {
                    var isBlacklisted = _context.JwtBlacklists
                        .AnyAsync(jb => jb.TokenId == tokenId && jb.ExpiresAt > DateTime.UtcNow)
                        .GetAwaiter()
                        .GetResult();

                    if (isBlacklisted)
                    {
                        _logger.LogWarning("Token validation failed: Token is blacklisted");
                        return false;
                    }
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtOptions.SecretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtOptions.Issuer,
                    ValidAudience = _jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, validationParameters, out _);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return false;
            }
        }

        public Guid? GetUserIdFromToken(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return null;
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);

                // JWT uses short claim types, so we need to check for "nameid" instead of the full URI
                var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting user ID from token");
                return null;
            }
        }

        private string GenerateAccessToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtOptions.SecretKey);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
                new Claim(ClaimTypes.Surname, user.LastName ?? string.Empty),
                new Claim("organization_id", user.OrganizationId.ToString()),
                new Claim("role", user.Role ?? "member"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private async Task<string> GenerateRefreshTokenAsync(Guid userId, string replacedByTokenHash = null)
        {
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = HashToken(Guid.NewGuid().ToString()),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
                IsRevoked = false,
                ReplacedByTokenHash = replacedByTokenHash ?? string.Empty
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken.TokenHash;
        }

        private string HashToken(string token)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(token);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private string GetTokenIdFromJwt(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);

                var jtiClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
                return jtiClaim?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting token ID from JWT");
                return null;
            }
        }

        private DateTime GetTokenExpirationFromJwt(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);
                return jsonToken.ValidTo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting expiration from JWT");
                return DateTime.UtcNow.AddMinutes(1); // Default to 1 minute from now
            }
        }
    }
}
