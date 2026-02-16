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

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="userService">The user service.</param>
        /// <param name="jwtOptions">The JWT options.</param>
        /// <param name="logger">The logger.</param>
        public AuthenticationService(
            SynaxisDbContext context,
            IUserService userService,
            IOptions<JwtOptions> jwtOptions,
            ILogger<AuthenticationService> logger)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._userService = userService ?? throw new ArgumentNullException(nameof(userService));
            this._jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<AuthenticationResult> AuthenticateAsync(string email, string password)
        {
            try
            {
                this._logger.LogInformation("Authentication attempt for email: {Email}", email);

                var validationResult = this.ValidateAuthenticationInput(email, password);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var user = await this._userService.AuthenticateAsync(email, password).ConfigureAwait(false);

                if (user.MfaEnabled)
                {
                    return this.CreateMfaRequiredResult(user);
                }

                return await this.CreateSuccessfulAuthenticationResultAsync(user).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException ex)
            {
                this._logger.LogWarning(ex, "Authentication failed for email: {Email}", email);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                };
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during authentication for email: {Email}", email);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred during authentication",
                };
            }
        }

        private AuthenticationResult? ValidateAuthenticationInput(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                this._logger.LogWarning("Authentication failed: Email is required");
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Email is required",
                };
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                this._logger.LogWarning("Authentication failed: Password is required");
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Password is required",
                };
            }

            return null;
        }

        private AuthenticationResult CreateMfaRequiredResult(User user)
        {
            this._logger.LogInformation("MFA required for user: {UserId}", user.Id);
            return new AuthenticationResult
            {
                Success = false,
                RequiresMfa = true,
                User = user,
                ErrorMessage = "MFA code required",
            };
        }

        private async Task<AuthenticationResult> CreateSuccessfulAuthenticationResultAsync(User user)
        {
            var accessToken = this.GenerateAccessToken(user);
            var refreshToken = await this.GenerateRefreshTokenAsync(user.Id).ConfigureAwait(false);

            this._logger.LogInformation("Authentication successful for user: {UserId}", user.Id);

            return new AuthenticationResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = this._jwtOptions.AccessTokenExpirationMinutes * 60,
                User = user,
                RequiresMfa = false,
            };
        }

        /// <inheritdoc/>
        public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                this._logger.LogInformation("Token refresh attempt");

                var validationResult = this.ValidateRefreshTokenInput(refreshToken);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var refreshTokenEntity = await this.GetRefreshTokenEntityAsync(refreshToken).ConfigureAwait(false);
                var validationError = this.ValidateRefreshTokenEntity(refreshTokenEntity);
                if (validationError != null)
                {
                    return validationError;
                }

                return await this.RefreshTokensAsync(refreshTokenEntity).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during token refresh");
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred during token refresh",
                };
            }
        }

        private AuthenticationResult? ValidateRefreshTokenInput(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                this._logger.LogWarning("Token refresh failed: Refresh token is required");
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Refresh token is required",
                };
            }

            return null;
        }

        private async Task<RefreshToken> GetRefreshTokenEntityAsync(string refreshToken)
        {
            var tokenHash = HashToken(refreshToken);
            var refreshTokenEntity = await this._context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash)
                .ConfigureAwait(false);

            if (refreshTokenEntity == null)
            {
                this._logger.LogWarning("Token refresh failed: Refresh token not found");
                throw new InvalidOperationException("Invalid refresh token");
            }

            return refreshTokenEntity;
        }

        private AuthenticationResult? ValidateRefreshTokenEntity(RefreshToken refreshTokenEntity)
        {
            if (refreshTokenEntity.IsRevoked)
            {
                this._logger.LogWarning("Token refresh failed: Refresh token has been revoked");
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Refresh token has been revoked",
                };
            }

            if (refreshTokenEntity.ExpiresAt < DateTime.UtcNow)
            {
                this._logger.LogWarning("Token refresh failed: Refresh token has expired");
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Refresh token has expired",
                };
            }

            if (refreshTokenEntity.User == null || !refreshTokenEntity.User.IsActive)
            {
                this._logger.LogWarning("Token refresh failed: User account is not active");
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "User account is not active",
                };
            }

            return null;
        }

        private async Task<AuthenticationResult> RefreshTokensAsync(RefreshToken refreshTokenEntity)
        {
            if (refreshTokenEntity.User == null)
            {
                throw new InvalidOperationException("User not found for refresh token");
            }

            var accessToken = this.GenerateAccessToken(refreshTokenEntity.User);
            var newRefreshToken = await this.GenerateRefreshTokenAsync(refreshTokenEntity.User.Id).ConfigureAwait(false);

            refreshTokenEntity.IsRevoked = true;
            refreshTokenEntity.RevokedAt = DateTime.UtcNow;
            refreshTokenEntity.ReplacedByTokenHash = newRefreshToken;

            await this._context.SaveChangesAsync().ConfigureAwait(false);

            this._logger.LogInformation("Token refresh successful for user: {UserId}", refreshTokenEntity.User.Id);

            return new AuthenticationResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = this._jwtOptions.AccessTokenExpirationMinutes * 60,
                User = refreshTokenEntity.User,
            };
        }

        /// <inheritdoc/>
        public async Task LogoutAsync(string refreshToken, string? accessToken = null)
        {
            try
            {
                this._logger.LogInformation("Logout attempt");

                // Invalidate JWT access token if provided
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    var tokenId = this.GetTokenIdFromJwt(accessToken);
                    if (tokenId != null)
                    {
                        var userId = this.GetUserIdFromToken(accessToken);
                        if (userId.HasValue)
                        {
                            var expiresAt = this.GetTokenExpirationFromJwt(accessToken);

                            var jwtBlacklist = new JwtBlacklist
                            {
                                Id = Guid.NewGuid(),
                                UserId = userId.Value,
                                TokenId = tokenId,
                                CreatedAt = DateTime.UtcNow,
                                ExpiresAt = expiresAt,
                            };

                            this._context.JwtBlacklists.Add(jwtBlacklist);
                            this._logger.LogInformation("JWT access token added to blacklist: {TokenId}", tokenId);
                        }
                    }
                }

                // Revoke refresh token if provided
                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    var tokenHash = HashToken(refreshToken);
                    var refreshTokenEntity = await this._context.RefreshTokens
                        .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash)
                        .ConfigureAwait(false);

                    if (refreshTokenEntity != null)
                    {
                        refreshTokenEntity.IsRevoked = true;
                        refreshTokenEntity.RevokedAt = DateTime.UtcNow;
                        this._logger.LogInformation("Refresh token revoked for user: {UserId}", refreshTokenEntity.UserId);
                    }
                    else
                    {
                        this._logger.LogWarning("Logout: Refresh token not found");
                    }
                }

                await this._context.SaveChangesAsync().ConfigureAwait(false);
                this._logger.LogInformation("Logout successful");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during logout");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return false;
                }

                // Check if token is blacklisted
                var tokenId = this.GetTokenIdFromJwt(token);
                if (tokenId != null)
                {
                    var isBlacklisted = await this._context.JwtBlacklists
                        .AnyAsync(jb => jb.TokenId == tokenId && jb.ExpiresAt > DateTime.UtcNow)
                        .ConfigureAwait(false);

                    if (isBlacklisted)
                    {
                        this._logger.LogWarning("Token validation failed: Token is blacklisted");
                        return false;
                    }
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(this._jwtOptions.SecretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = this._jwtOptions.Issuer,
                    ValidAudience = this._jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero,
                };

                tokenHandler.ValidateToken(token, validationParameters, out _);
                return true;
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex, "Token validation failed");
                return false;
            }
        }

        /// <inheritdoc/>
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
                var userIdClaim = jsonToken.Claims.FirstOrDefault(c => string.Equals(c.Type, "nameid", StringComparison.Ordinal) || string.Equals(c.Type, ClaimTypes.NameIdentifier, StringComparison.Ordinal));
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }

                return null;
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex, "Error extracting user ID from token");
                return null;
            }
        }

        private string GenerateAccessToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(this._jwtOptions.SecretKey);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
                new Claim(ClaimTypes.Surname, user.LastName ?? string.Empty),
                new Claim("organization_id", user.OrganizationId.ToString()),
                new Claim("role", user.Role ?? "member"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(this._jwtOptions.AccessTokenExpirationMinutes),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = this._jwtOptions.Issuer,
                Audience = this._jwtOptions.Audience,
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private async Task<string> GenerateRefreshTokenAsync(Guid userId, string? replacedByTokenHash = null)
        {
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = HashToken(Guid.NewGuid().ToString()),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(this._jwtOptions.RefreshTokenExpirationDays),
                IsRevoked = false,
                ReplacedByTokenHash = replacedByTokenHash ?? string.Empty,
            };

            this._context.RefreshTokens.Add(refreshToken);
            await this._context.SaveChangesAsync().ConfigureAwait(false);

            return refreshToken.TokenHash;
        }

        private static string HashToken(string token)
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

                var jtiClaim = jsonToken.Claims.FirstOrDefault(c => string.Equals(c.Type, JwtRegisteredClaimNames.Jti, StringComparison.Ordinal));
                return jtiClaim?.Value ?? string.Empty;
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex, "Error extracting token ID from JWT");
                return string.Empty;
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
                this._logger.LogWarning(ex, "Error extracting expiration from JWT");
                return DateTime.UtcNow.AddMinutes(1); // Default to 1 minute from now
            }
        }
    }
}
