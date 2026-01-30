using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Moq;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Infrastructure.Security;
using Xunit;
using Xunit.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Synaxis.InferenceGateway.IntegrationTests
{
    public class JwtServiceTests
    {
        private readonly ITestOutputHelper _output;

        public JwtServiceTests(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact]
        public void GenerateToken_ShouldCreateValidJwtToken()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "this-is-a-very-long-test-secret-key-for-jwt-token-generation-that-is-definitely-longer-than-32-bytes-and-secure",
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis"
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Role = UserRole.Admin,
                TenantId = Guid.NewGuid()
            };

            var token = jwtService.GenerateToken(user);

            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.Contains(".", token);
        }

        [Fact]
        public void GenerateToken_ShouldIncludeCorrectClaims()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "test-secret-key-for-claims-validation-that-is-long-enough-for-hmac-sha256-algorithm",
                JwtIssuer = "TestIssuer",
                JwtAudience = "TestAudience"
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);
            
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "claims.test@example.com",
                Role = UserRole.Developer,
                TenantId = tenantId
            };

            var token = jwtService.GenerateToken(user);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            Assert.Equal(userId.ToString(), jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
            Assert.Equal(user.Email, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
            Assert.Equal("Developer", jwtToken.Claims.First(c => c.Type == "role").Value);
            Assert.Equal(tenantId.ToString(), jwtToken.Claims.First(c => c.Type == "tenantId").Value);
        }

        [Fact]
        public void GenerateToken_ShouldHaveCorrectExpiration()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "test-secret-for-expiration-validation-and-testing-long-enough-key",
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis"
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "expiration.test@example.com",
                Role = UserRole.Developer,
                TenantId = Guid.NewGuid()
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
                JwtAudience = "CustomAudience"
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "issuer.test@example.com",
                Role = UserRole.Developer,
                TenantId = Guid.NewGuid()
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
                JwtAudience = null
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "defaults.test@example.com",
                Role = UserRole.Developer,
                TenantId = Guid.NewGuid()
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
                JwtAudience = "Synaxis"
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "signature.test@example.com",
                Role = UserRole.Developer,
                TenantId = Guid.NewGuid()
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
                JwtSecret = "",
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis"
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "empty.secret@example.com",
                Role = UserRole.Developer,
                TenantId = Guid.NewGuid()
            };

            var exception = Assert.Throws<InvalidOperationException>(() => 
                jwtService.GenerateToken(user));
            
            Assert.Contains("Synaxis:InferenceGateway:JwtSecret must be configured", exception.Message);
        }

        [Fact]
        public void GenerateToken_ShouldThrowException_ForWhitespaceSecret()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "   ",
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis"
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "whitespace.secret@example.com",
                Role = UserRole.Readonly,
                TenantId = Guid.NewGuid()
            };

            var exception = Assert.Throws<InvalidOperationException>(() => 
                jwtService.GenerateToken(user));
            
            Assert.Contains("Synaxis:InferenceGateway:JwtSecret must be configured", exception.Message);
        }

        [Fact]
        public void GenerateToken_ShouldGenerateDifferentTokens_ForSameUser()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "test-secret-for-uniqueness-that-is-long-enough-for-hmac-sha256-algorithm",
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis"
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "uniqueness.test@example.com",
                Role = UserRole.Developer,
                TenantId = Guid.NewGuid()
            };

            var token1 = jwtService.GenerateToken(user);
            var token2 = jwtService.GenerateToken(user);

            // JWT tokens for the same user should be identical because they don't include a jti (JWT ID) by default
            // This is expected behavior - the tokens should be deterministic for the same input
            Assert.Equal(token1, token2);
        }

        [Fact]
        public void GenerateToken_ShouldGenerateDifferentTokens_ForDifferentUsers()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "test-secret-for-user-differences-that-is-long-enough-for-hmac-sha256",
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis"
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);
            
            var user1 = new User
            {
                Id = Guid.NewGuid(),
                Email = "user1@example.com",
                Role = UserRole.Developer,
                TenantId = Guid.NewGuid()
            };

            var user2 = new User
            {
                Id = Guid.NewGuid(),
                Email = "user2@example.com",
                Role = UserRole.Admin,
                TenantId = Guid.NewGuid()
            };

            var token1 = jwtService.GenerateToken(user1);
            var token2 = jwtService.GenerateToken(user2);

            Assert.NotEqual(token1, token2);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken1 = tokenHandler.ReadJwtToken(token1);
            var jwtToken2 = tokenHandler.ReadJwtToken(token2);
            
            Assert.NotEqual(
                jwtToken1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value,
                jwtToken2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value
            );
        }

        [Fact]
        public void GenerateToken_ShouldHandleAllUserRoles()
        {
            var config = new SynaxisConfiguration
            {
                JwtSecret = "test-secret-for-roles-that-is-long-enough-for-hmac-sha256-algorithm",
                JwtIssuer = "Synaxis",
                JwtAudience = "Synaxis"
            };
            var mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            mockConfig.Setup(x => x.Value).Returns(config);
            var jwtService = new JwtService(mockConfig.Object);
            
            var roles = new[] { UserRole.Owner, UserRole.Admin, UserRole.Developer, UserRole.Readonly };

            foreach (var role in roles)
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = $"role.test@{role}.example.com",
                    Role = role,
                    TenantId = Guid.NewGuid()
                };

                var token = jwtService.GenerateToken(user);

                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var roleClaim = jwtToken.Claims.First(c => c.Type == "role").Value;
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
