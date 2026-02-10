// <copyright file="SecurityConfigurationValidatorTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Security;

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Infrastructure.Security;
using Xunit;

public class SecurityConfigurationValidatorTests
{
    private readonly Mock<ILogger<SecurityConfigurationValidator>> _mockLogger;

    public SecurityConfigurationValidatorTests()
    {
        this._mockLogger = new Mock<ILogger<SecurityConfigurationValidator>>();
    }

    [Fact]
    public void Validate_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(
StringComparer.Ordinal)
            {
                ["Synaxis:InferenceGateway:JwtSecret"] = "ThisIsAVerySecureJwtSecretKey123456789012345",
                ["Synaxis:InferenceGateway:Providers:Groq:RateLimitRPM"] = "100",
                ["Synaxis:InferenceGateway:Cors:WebAppOrigins"] = "http://localhost:8080",
            })
            .Build();

        var validator = new SecurityConfigurationValidator(configuration, this._mockLogger.Object, "Production");

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.HasErrors.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMissingJwtSecret_ReturnsError()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal))
            .Build();

        var validator = new SecurityConfigurationValidator(configuration, this._mockLogger.Object, "Production");

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("JWT Secret is not configured"));
    }

    [Fact]
    public void Validate_WithShortJwtSecret_ReturnsError()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(
StringComparer.Ordinal)
            {
                ["Synaxis:InferenceGateway:JwtSecret"] = "short",
            })
            .Build();

        var validator = new SecurityConfigurationValidator(configuration, this._mockLogger.Object, "Production");

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("at least 32 characters"));
    }

    [Fact]
    public void Validate_WithDefaultSecretInDevelopment_ReturnsWarning()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(
StringComparer.Ordinal)
            {
                ["Synaxis:InferenceGateway:JwtSecret"] = "SynaxisDefaultSecretKeyDoNotUseInProd1234567890",
            })
            .Build();

        var validator = new SecurityConfigurationValidator(configuration, this._mockLogger.Object, "Development");

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.HasWarnings.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("Default JWT secret"));
    }

    [Fact]
    public void Validate_WithDefaultSecretInProduction_ReturnsError()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(
StringComparer.Ordinal)
            {
                ["Synaxis:InferenceGateway:JwtSecret"] = "SynaxisDefaultSecretKeyDoNotUseInProd1234567890",
            })
            .Build();

        var validator = new SecurityConfigurationValidator(configuration, this._mockLogger.Object, "Production");

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Default JWT secret detected"));
    }

    [Fact]
    public void Validate_WithWeakJwtSecret_ReturnsWarning()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(
StringComparer.Ordinal)
            {
                ["Synaxis:InferenceGateway:JwtSecret"] = "mypassword123456789012345678901234",
            })
            .Build();

        var validator = new SecurityConfigurationValidator(configuration, this._mockLogger.Object, "Production");

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.HasWarnings.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("weak patterns"));
    }

    [Fact]
    public void Validate_WithNoRateLimitingInProduction_ReturnsWarning()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(
StringComparer.Ordinal)
            {
                ["Synaxis:InferenceGateway:JwtSecret"] = "ThisIsAVerySecureJwtSecretKey123456789012345",
                ["Synaxis:InferenceGateway:Providers:Groq:Enabled"] = "true",
            })
            .Build();

        var validator = new SecurityConfigurationValidator(configuration, this._mockLogger.Object, "Production");

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.HasWarnings.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("No rate limiting configured"));
    }

    [Fact]
    public void Validate_WithWildcardCorsInProduction_ReturnsError()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(
StringComparer.Ordinal)
            {
                ["Synaxis:InferenceGateway:JwtSecret"] = "ThisIsAVerySecureJwtSecretKey123456789012345",
                ["Synaxis:InferenceGateway:Cors:PublicOrigins"] = "*",
            })
            .Build();

        var validator = new SecurityConfigurationValidator(configuration, this._mockLogger.Object, "Production");

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("Wildcard (*) CORS origin"));
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(
StringComparer.Ordinal)
            {
                ["Synaxis:InferenceGateway:JwtSecret"] = "short",
                ["Synaxis:InferenceGateway:Cors:PublicOrigins"] = "*",
            })
            .Build();

        var validator = new SecurityConfigurationValidator(configuration, this._mockLogger.Object, "Production");

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Theory]
    [InlineData("password123456789012345678901234")]
    [InlineData("secret123456789012345678901234567")]
    [InlineData("test1234567890123456789012345678")]
    [InlineData("admin123456789012345678901234567")]
    public void Validate_WithCommonWeakSecrets_ReturnsWarning(string weakSecret)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(
StringComparer.Ordinal)
            {
                ["Synaxis:InferenceGateway:JwtSecret"] = weakSecret,
            })
            .Build();

        var validator = new SecurityConfigurationValidator(configuration, this._mockLogger.Object, "Production");

        // Act
        var result = validator.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.HasWarnings.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("weak patterns"));
    }
}
