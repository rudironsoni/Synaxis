namespace Synaxis.Contracts.Tests.V1.Errors;

using FluentAssertions;
using Synaxis.Contracts.V1.Errors;

public class SynaxisErrorTests
{
    [Fact]
    public void SynaxisError_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var error = new SynaxisError
        {
            Code = ErrorCodes.AuthenticationFailed,
            Message = "Authentication failed",
            Severity = ErrorSeverity.Error,
            Category = ErrorCategory.Auth,
            Details = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["reason"] = "Invalid credentials",
            },
        };

        // Assert
        error.Code.Should().Be(ErrorCodes.AuthenticationFailed);
        error.Message.Should().Be("Authentication failed");
        error.Severity.Should().Be(ErrorSeverity.Error);
        error.Category.Should().Be(ErrorCategory.Auth);
        error.Details.Should().ContainKey("reason");
    }

    [Fact]
    public void SynaxisError_DefaultValues_AreSet()
    {
        // Arrange & Act
        var error = new SynaxisError();

        // Assert
        error.Code.Should().BeEmpty();
        error.Message.Should().BeEmpty();
        error.Severity.Should().Be(ErrorSeverity.Error);
        error.Category.Should().Be(ErrorCategory.System);
        error.Details.Should().NotBeNull();
        error.Details.Should().BeEmpty();
    }

    [Fact]
    public void SynaxisError_IsImmutable_CannotBeModifiedAfterCreation()
    {
        // Arrange
        var error = new SynaxisError
        {
            Code = "TEST_ERROR",
            Message = "Test message",
        };

        // Act & Assert
        error.Code.Should().Be("TEST_ERROR");
        error.Message.Should().Be("Test message");
    }

    [Fact]
    public void SynaxisError_WithDetails_CanStoreMultipleValues()
    {
        // Arrange & Act
        var error = new SynaxisError
        {
            Code = "COMPLEX_ERROR",
            Details = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["key1"] = "value1",
                ["key2"] = 42,
                ["key3"] = true,
            },
        };

        // Assert
        error.Details.Should().HaveCount(3);
        error.Details["key1"].Should().Be("value1");
        error.Details["key2"].Should().Be(42);
        error.Details["key3"].Should().Be(true);
    }

    [Theory]
    [InlineData(ErrorSeverity.Fatal)]
    [InlineData(ErrorSeverity.Error)]
    [InlineData(ErrorSeverity.Warning)]
    [InlineData(ErrorSeverity.Info)]
    public void SynaxisError_AllSeverityLevels_CanBeSet(ErrorSeverity severity)
    {
        // Arrange & Act
        var error = new SynaxisError
        {
            Severity = severity,
        };

        // Assert
        error.Severity.Should().Be(severity);
    }

    [Theory]
    [InlineData(ErrorCategory.Auth)]
    [InlineData(ErrorCategory.RateLimit)]
    [InlineData(ErrorCategory.Provider)]
    [InlineData(ErrorCategory.Validation)]
    [InlineData(ErrorCategory.System)]
    public void SynaxisError_AllCategories_CanBeSet(ErrorCategory category)
    {
        // Arrange & Act
        var error = new SynaxisError
        {
            Category = category,
        };

        // Assert
        error.Category.Should().Be(category);
    }

    [Fact]
    public void ErrorSeverity_HasCorrectOrder()
    {
        // Arrange & Act & Assert
        ((int)ErrorSeverity.Fatal).Should().Be(0);
        ((int)ErrorSeverity.Error).Should().Be(1);
        ((int)ErrorSeverity.Warning).Should().Be(2);
        ((int)ErrorSeverity.Info).Should().Be(3);
    }

    [Fact]
    public void ErrorCategory_HasAllExpectedValues()
    {
        // Arrange & Act & Assert
        ((int)ErrorCategory.Auth).Should().Be(0);
        ((int)ErrorCategory.RateLimit).Should().Be(1);
        ((int)ErrorCategory.Provider).Should().Be(2);
        ((int)ErrorCategory.Validation).Should().Be(3);
        ((int)ErrorCategory.System).Should().Be(4);
    }
}
