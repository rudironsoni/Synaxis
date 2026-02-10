// <copyright file="JwtServiceTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Synaxis.Core.Models;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Infrastructure.Security;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests
{
    public class JwtServiceTests
    {
        public JwtServiceTests(ITestOutputHelper output)
        {
            _ = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact]
        public void GenerateToken_ShouldCreateValidJwtToken()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "this-is-a-very-long-test-secret-key-for-jwt-token-generation-that-is-definitely-longer-than-32-bytes-and-secure",
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis",
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Role = "admin",
                OrganizationId = Guid.NewGuid(),
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                PasswordHash = "dummy",
            };

            var token = jwtService.GenerateToken(user);

            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.Contains(".", token, StringComparison.Ordinal);
        }

        [Fact]
        public void GenerateToken_ShouldIncludeCorrectClaims()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "test-secret-key-for-claims-validation-that-is-long-enough-for-hmac-sha256-algorithm",
                JwtIssuer = "TestIssuer",
                JwtAudience = "TestAudience",
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);

            var userId = Guid.NewGuid();
            var organizationId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "claims.test@example.com",
                Role = "developer",
                OrganizationId = organizationId,
            };

            var token = jwtService.GenerateToken(user);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            Assert.Equal(userId.ToString(), jwtToken.Claims.First(c => string.Equals(c.Type, JwtRegisteredClaimNames.Sub, StringComparison.Ordinal)).Value);
            Assert.Equal(user.Email, jwtToken.Claims.First(c => string.Equals(c.Type, JwtRegisteredClaimNames.Email, StringComparison.Ordinal)).Value);
            Assert.Equal("developer", jwtToken.Claims.First(c => string.Equals(c.Type, "role", StringComparison.Ordinal)).Value);
            Assert.Equal(organizationId.ToString(), jwtToken.Claims.First(c => string.Equals(c.Type, "organizationId", StringComparison.Ordinal)).Value);
        }

        [Fact]
        public void GenerateToken_ShouldHaveCorrectExpiration()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "test-secret-for-expiration-validation-and-testing-long-enough-key",
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis",
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "expiration.test@example.com",
                Role = "developer",
                OrganizationId = Guid.NewGuid(),
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                PasswordHash = "dummy",
            };

            var token = jwtService.GenerateToken(user);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            var expectedExpiration = DateTime.UtcNow.AddDays(7);
            var actualExpiration = jwtToken.ValidTo;

            var timeDiff = Math.Abs((actualExpiration - expectedExpiration).TotalSeconds);
            Assert.True(timeDiff < 10, $"Expiration time difference too large: {timeDiff} seconds");
        }

        [Fact]
        public void GenerateToken_ShouldUseConfiguredIssuerAndAudience()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "test-secret-for-issuer-audience-that-is-long-enough-for-hmac-sha256",
                JwtIssuer = "CustomIssuer",
                JwtAudience = "CustomAudience",
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "issuer.test@example.com",
                Role = "developer",
                OrganizationId = Guid.NewGuid(),
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                PasswordHash = "dummy",
            };

            var token = jwtService.GenerateToken(user);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            Assert.Equal("CustomIssuer", jwtToken.Issuer);
            Assert.Equal("CustomAudience", jwtToken.Audiences.First());
        }

        [Fact]
        public void GenerateToken_ShouldUseDefaultIssuerAndAudienceWhenNull()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "test-secret-for-defaults-that-is-long-enough-for-hmac-sha256-algorithm",
                JwtIssuer = null,
                JwtAudience = null,
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "defaults.test@example.com",
                Role = "developer",
                OrganizationId = Guid.NewGuid(),
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                PasswordHash = "dummy",
            };

            var token = jwtService.GenerateToken(user);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            Assert.Equal("Synaxis", jwtToken.Issuer);
            Assert.Equal("Synaxis", jwtToken.Audiences.First());
        }

        [Fact]
        public void GenerateToken_ShouldUseHmacSha256Signature()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "test-secret-for-signature-validation-that-is-long-enough-for-hmac-sha256",
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis",
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "signature.test@example.com",
                Role = "developer",
                OrganizationId = Guid.NewGuid(),
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                PasswordHash = "dummy",
            };

            var token = jwtService.GenerateToken(user);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            Assert.Equal(SecurityAlgorithms.HmacSha256, jwtToken.Header.Alg);
        }

        [Fact]
        public void GenerateToken_ShouldThrowException_ForEmptySecret()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = string.Empty,
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis",
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "empty.secret@example.com",
                Role = "developer",
                OrganizationId = Guid.NewGuid(),
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                PasswordHash = "dummy",
            };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                jwtService.GenerateToken(user));

            Assert.Contains("Synaxis:InferenceGateway:JwtSecret must be configured", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void GenerateToken_ShouldThrowException_ForWhitespaceSecret()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "   ",
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis",
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "whitespace.secret@example.com",
                Role = "readonly",
                OrganizationId = Guid.NewGuid(),
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                PasswordHash = "dummy",
            };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                jwtService.GenerateToken(user));

            Assert.Contains("Synaxis:InferenceGateway:JwtSecret must be configured", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void GenerateToken_ShouldGenerateDifferentTokens_ForSameUser()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "test-secret-for-uniqueness-that-is-long-enough-for-hmac-sha256-algorithm",
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis",
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "uniqueness.test@example.com",
                Role = "developer",
                OrganizationId = Guid.NewGuid(),
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                PasswordHash = "dummy",
            };

            var token1 = jwtService.GenerateToken(user);
            var token2 = jwtService.GenerateToken(user);

            // JWT tokens for the same user should be different because they include a unique jti (JWT ID) claim
            // This is expected behavior - each token should be unique even for the same user
            Assert.NotEqual(token1, token2);
        }

        [Fact]
        public void GenerateToken_ShouldGenerateDifferentTokens_ForDifferentUsers()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "test-secret-for-user-differences-that-is-long-enough-for-hmac-sha256",
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis",
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);

            var user1 = new User
            {
                Id = Guid.NewGuid(),
                Email = "user1@example.com",
                Role = "developer",
                OrganizationId = Guid.NewGuid(),
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                PasswordHash = "dummy",
            };

            var user2 = new User
            {
                Id = Guid.NewGuid(),
                Email = "user2@example.com",
                Role = "admin",
                OrganizationId = Guid.NewGuid(),
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                PasswordHash = "dummy",
            };

            var token1 = jwtService.GenerateToken(user1);
            var token2 = jwtService.GenerateToken(user2);

            Assert.NotEqual(token1, token2);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken1 = tokenHandler.ReadJwtToken(token1);
            var jwtToken2 = tokenHandler.ReadJwtToken(token2);

            Assert.NotEqual(
                jwtToken1.Claims.First(c => string.Equals(c.Type, JwtRegisteredClaimNames.Sub, StringComparison.Ordinal)).Value,
                jwtToken2.Claims.First(c => string.Equals(c.Type, JwtRegisteredClaimNames.Sub, StringComparison.Ordinal)).Value);
        }

        [Fact]
        public void GenerateToken_ShouldHandleAllUserRoles()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "test-secret-for-roles-that-is-long-enough-for-hmac-sha256-algorithm",
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis",
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);

            var roles = new[] { "owner", "admin", "developer", "readonly" };

            foreach (var role in roles)
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = $"role.test@{role}.example.com",
                    Role = role,
                    OrganizationId = Guid.NewGuid(),
                    DataResidencyRegion = "us-east-1",
                    CreatedInRegion = "us-east-1",
                    PasswordHash = "dummy",
                };

                var token = jwtService.GenerateToken(user);

                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var roleClaim = jwtToken.Claims.First(c => string.Equals(c.Type, "role", StringComparison.Ordinal)).Value;
                Assert.Equal(role.ToString(), roleClaim);
            }
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_ForNullOptions()
        {
            Assert.Throws<ArgumentNullException>(() => new JwtService(null!));
        }
    }
}
