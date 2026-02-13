// <copyright file="TestJwtGenerator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.IntegrationTests.Helpers
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    /// Helper class for generating test JWT tokens for WebSocket authentication.
    /// </summary>
    public static class TestJwtGenerator
    {
        /// <summary>
        /// The test JWT secret key used in integration tests.
        /// Must match the key configured in SynaxisWebApplicationFactory.
        /// </summary>
        public const string TestJwtSecret = "TestJwtSecretKeyThatIsAtLeast32BytesLongForHmacSha256Algorithm";

        /// <summary>
        /// Generates a test JWT token for the specified user and organization.
        /// </summary>
        /// <param name="userId">The user ID to include in the token.</param>
        /// <param name="email">The email to include in the token.</param>
        /// <param name="organizationId">The organization ID to include in the token.</param>
        /// <param name="role">The role to include in the token. Default is "User".</param>
        /// <param name="expiresIn">Token expiration duration. Default is 1 hour.</param>
        /// <returns>A JWT token string.</returns>
        public static string GenerateToken(
            Guid userId,
            string email = "test@example.com",
            Guid? organizationId = null,
            string role = "User",
            TimeSpan? expiresIn = null)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("role", role),
                new Claim("organizationId", (organizationId ?? Guid.NewGuid()).ToString()),
            };

            var now = DateTime.UtcNow;
            var expiration = now.Add(expiresIn ?? TimeSpan.FromHours(1));

            // When creating expired tokens, set NotBefore before expiration to avoid JWT validation error
            var notBefore = expiration < now ? expiration.AddMinutes(-5) : now;

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = notBefore,
                Expires = expiration,
                Issuer = "Synaxis",
                Audience = "Synaxis",
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }
    }
}
