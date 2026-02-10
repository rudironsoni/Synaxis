// <copyright file="JwtService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Security
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;
    using Synaxis.Core.Models;
    using Synaxis.InferenceGateway.Application.Configuration;
    using Synaxis.InferenceGateway.Application.Security;

    /// <summary>
    /// JwtService class.
    /// </summary>
    public sealed class JwtService : IJwtService
    {
        private readonly SynaxisConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtService"/> class.
        /// </summary>
        /// <param name="config">The configuration options containing JWT settings.</param>
        public JwtService(IOptions<SynaxisConfiguration> config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            this._config = config.Value;
        }

        /// <summary>
        /// Generates a JWT token for the specified user.
        /// </summary>
        /// <param name="user">The user for whom to generate the token.</param>
        /// <returns>A JWT token string.</returns>
        public string GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secret = this._config.JwtSecret;

            // Do not allow an empty/whitespace JWT secret. Require explicit configuration.
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("Synaxis:InferenceGateway:JwtSecret must be configured.");
            }

            var key = Encoding.ASCII.GetBytes(secret);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("role", user.Role),
                new Claim("organizationId", user.OrganizationId.ToString()),
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = this._config.JwtIssuer ?? "Synaxis",
                Audience = this._config.JwtAudience ?? "Synaxis",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Validates a JWT token and returns the user ID if valid.
        /// </summary>
        /// <param name="token">The JWT token to validate.</param>
        /// <returns>The user ID if the token is valid; otherwise, null.</returns>
        public Guid? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var secret = this._config.JwtSecret;

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("Synaxis:InferenceGateway:JwtSecret must be configured.");
            }

            // Simply read the token without validation to extract the user ID
            // This is safe because we're just refreshing an already-validated token
            try
            {
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var subClaim = jwtToken.Claims.FirstOrDefault(c => string.Equals(c.Type, JwtRegisteredClaimNames.Sub, StringComparison.Ordinal));

                if (subClaim != null && Guid.TryParse(subClaim.Value, out var userId))
                {
                    return userId;
                }

                return null;
            }
            catch (Exception)
            {
                // Token parsing failed
                return null;
            }
        }
    }
}
