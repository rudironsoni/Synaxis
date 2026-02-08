namespace Synaxis.Contracts.Tests.V1.Errors;

using FluentAssertions;
using Synaxis.Contracts.V1.Errors;

public class ErrorCodesTests
{
    [Theory]
    [InlineData(ErrorCodes.AuthenticationFailed, "AUTH_FAILED")]
    [InlineData(ErrorCodes.AuthorizationDenied, "AUTH_DENIED")]
    [InlineData(ErrorCodes.InvalidApiKey, "AUTH_INVALID_API_KEY")]
    public void AuthErrorCodes_HaveCorrectValues(string code, string expectedValue)
    {
        // Assert
        code.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(ErrorCodes.RateLimitExceeded, "RATE_LIMIT_EXCEEDED")]
    [InlineData(ErrorCodes.QuotaExceeded, "RATE_QUOTA_EXCEEDED")]
    public void RateLimitErrorCodes_HaveCorrectValues(string code, string expectedValue)
    {
        // Assert
        code.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(ErrorCodes.ProviderUnavailable, "PROVIDER_UNAVAILABLE")]
    [InlineData(ErrorCodes.ProviderError, "PROVIDER_ERROR")]
    [InlineData(ErrorCodes.ProviderTimeout, "PROVIDER_TIMEOUT")]
    [InlineData(ErrorCodes.ModelNotSupported, "PROVIDER_MODEL_NOT_SUPPORTED")]
    public void ProviderErrorCodes_HaveCorrectValues(string code, string expectedValue)
    {
        // Assert
        code.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(ErrorCodes.ValidationFailed, "VALIDATION_FAILED")]
    [InlineData(ErrorCodes.RequiredFieldMissing, "VALIDATION_REQUIRED_FIELD_MISSING")]
    [InlineData(ErrorCodes.InvalidFieldValue, "VALIDATION_INVALID_FIELD_VALUE")]
    public void ValidationErrorCodes_HaveCorrectValues(string code, string expectedValue)
    {
        // Assert
        code.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(ErrorCodes.InternalError, "SYSTEM_INTERNAL_ERROR")]
    [InlineData(ErrorCodes.NotFound, "SYSTEM_NOT_FOUND")]
    [InlineData(ErrorCodes.ServiceUnavailable, "SYSTEM_SERVICE_UNAVAILABLE")]
    public void SystemErrorCodes_HaveCorrectValues(string code, string expectedValue)
    {
        // Assert
        code.Should().Be(expectedValue);
    }

    [Fact]
    public void ErrorCodes_AllAuthCodes_StartWithAuthPrefix()
    {
        // Assert
        ErrorCodes.AuthenticationFailed.Should().StartWith("AUTH_");
        ErrorCodes.AuthorizationDenied.Should().StartWith("AUTH_");
        ErrorCodes.InvalidApiKey.Should().StartWith("AUTH_");
    }

    [Fact]
    public void ErrorCodes_AllRateCodes_StartWithRatePrefix()
    {
        // Assert
        ErrorCodes.RateLimitExceeded.Should().StartWith("RATE_");
        ErrorCodes.QuotaExceeded.Should().StartWith("RATE_");
    }

    [Fact]
    public void ErrorCodes_AllProviderCodes_StartWithProviderPrefix()
    {
        // Assert
        ErrorCodes.ProviderUnavailable.Should().StartWith("PROVIDER_");
        ErrorCodes.ProviderError.Should().StartWith("PROVIDER_");
        ErrorCodes.ProviderTimeout.Should().StartWith("PROVIDER_");
        ErrorCodes.ModelNotSupported.Should().StartWith("PROVIDER_");
    }

    [Fact]
    public void ErrorCodes_AllValidationCodes_StartWithValidationPrefix()
    {
        // Assert
        ErrorCodes.ValidationFailed.Should().StartWith("VALIDATION_");
        ErrorCodes.RequiredFieldMissing.Should().StartWith("VALIDATION_");
        ErrorCodes.InvalidFieldValue.Should().StartWith("VALIDATION_");
    }

    [Fact]
    public void ErrorCodes_AllSystemCodes_StartWithSystemPrefix()
    {
        // Assert
        ErrorCodes.InternalError.Should().StartWith("SYSTEM_");
        ErrorCodes.NotFound.Should().StartWith("SYSTEM_");
        ErrorCodes.ServiceUnavailable.Should().StartWith("SYSTEM_");
    }

    [Fact]
    public void ErrorCodes_AreConstants_NotEmpty()
    {
        // Assert
        ErrorCodes.AuthenticationFailed.Should().NotBeNullOrEmpty();
        ErrorCodes.RateLimitExceeded.Should().NotBeNullOrEmpty();
        ErrorCodes.ProviderError.Should().NotBeNullOrEmpty();
        ErrorCodes.ValidationFailed.Should().NotBeNullOrEmpty();
        ErrorCodes.InternalError.Should().NotBeNullOrEmpty();
    }
}
