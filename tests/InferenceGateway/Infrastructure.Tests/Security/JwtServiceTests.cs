using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Infrastructure.Security;
using Xunit;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Security
{
    public class JwtServiceTests
    {
        private readonly Mock<IOptions<SynaxisConfiguration>> _mockConfig;
        private readonly JwtService _jwtService;
        private readonly SynaxisConfiguration _config;

        public JwtServiceTests()
        {
            _config = new SynaxisConfiguration
            {
                JwtSecret = "THIS_IS_A_VERY_LONG_SECRET_KEY_FOR_TESTING_PURPOSES_ONLY_1234567890",
                JwtIssuer = "TestIssuer",
                JwtAudience = "TestAudience"
            };

            _mockConfig = new Mock<IOptions<SynaxisConfiguration>>();
            _mockConfig.Setup(c => c.Value).Returns(_config);

            _jwtService = new JwtService(_mockConfig.Object);
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Arrange
            IOptions<SynaxisConfiguration> nullConfig = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new JwtService(nullConfig!));
        }

        [Fact]
        public void GenerateToken_WithValidUser_ReturnsValidJwtToken()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Email = "test@example.com",
                Role = UserRole.Developer
            };

            // Act
            var token = _jwtService.GenerateToken(user);

            // Assert
            Assert.False(string.IsNullOrEmpty(token));

            // Validate the token
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
            Assert.NotEmpty(principal.Claims);
        }

        [Fact]
        public void GenerateToken_WithEmptyJwtSecret_ThrowsInvalidOperationException()
        {
            // Arrange
            _config.JwtSecret = "";
            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Email = "test@example.com",
                Role = UserRole.Developer
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _jwtService.GenerateToken(user));
            Assert.Equal("Synaxis:InferenceGateway:JwtSecret must be configured.", exception.Message);
        }

        [Fact]
        public void GenerateToken_WithWhitespaceJwtSecret_ThrowsInvalidOperationException()
        {
            // Arrange
            _config.JwtSecret = "   ";
            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Email = "test@example.com",
                Role = UserRole.Developer
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _jwtService.GenerateToken(user));
            Assert.Equal("Synaxis:InferenceGateway:JwtSecret must be configured.", exception.Message);
        }

        [Fact]
        public void GenerateToken_WithNullJwtSecret_ThrowsInvalidOperationException()
        {
            // Arrange
            _config.JwtSecret = null;
            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Email = "test@example.com",
                Role = UserRole.Developer
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _jwtService.GenerateToken(user));
            Assert.Equal("Synaxis:InferenceGateway:JwtSecret must be configured.", exception.Message);
        }

        [Fact]
        public void GenerateToken_TokenExpiresInSevenDays()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Email = "test@example.com",
                Role = UserRole.Developer
            };

            // Act
            var token = _jwtService.GenerateToken(user);
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Assert
            var expectedExpiration = DateTime.UtcNow.AddDays(7);
            // Allow for small time differences
            var expirationDifference = Math.Abs((jwtToken.ValidTo - expectedExpiration).TotalSeconds);
            Assert.True(expirationDifference < 5); // Within 5 seconds
        }

        [Fact]
        public void GenerateToken_UsesConfiguredIssuerAndAudience()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Email = "test@example.com",
                Role = UserRole.Developer
            };

            // Act
            var token = _jwtService.GenerateToken(user);
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Assert
            Assert.Equal(_config.JwtIssuer, jwtToken.Issuer);
            Assert.Equal(_config.JwtAudience, jwtToken.Audiences.First());
        }

        [Fact]
        public void GenerateToken_WithNullIssuerAndAudience_UsesDefaults()
        {
            // Arrange
            _config.JwtIssuer = null;
            _config.JwtAudience = null;
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Email = "test@example.com",
                Role = UserRole.Developer
            };

            // Act
            var token = _jwtService.GenerateToken(user);
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Assert
            Assert.Equal("Synaxis", jwtToken.Issuer);
            Assert.Equal("Synaxis", jwtToken.Audiences.First());
        }
    }
}