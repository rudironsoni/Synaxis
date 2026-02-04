using Xunit;
using FluentAssertions;
using Synaxis.InferenceGateway.Application.Validation;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.InferenceGateway.Application.Tests.Validation;

public class OpenAIRequestValidatorTests
{
    [Fact]
    public void ValidateRequest_WithValidParameters_ReturnsSuccess()
    {
        // Arrange
        var model = "gpt-4";
        var messages = new[] { new { role = "user", content = "Hello" } };
        var temperature = 0.7;
        var maxTokens = 1000;

        // Act
        var result = OpenAIRequestValidator.ValidateRequest(model, messages, temperature, maxTokens);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateRequest_WithNullModel_ReturnsError()
    {
        // Arrange
        string? model = null;
        var messages = new[] { new { role = "user", content = "Hello" } };

        // Act
        var result = OpenAIRequestValidator.ValidateRequest(model, messages, null, null);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("Model is required");
    }

    [Fact]
    public void ValidateRequest_WithEmptyModel_ReturnsError()
    {
        // Arrange
        var model = "   ";
        var messages = new[] { new { role = "user", content = "Hello" } };

        // Act
        var result = OpenAIRequestValidator.ValidateRequest(model, messages, null, null);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("Model is required");
    }

    [Fact]
    public void ValidateRequest_WithNullMessages_ReturnsError()
    {
        // Arrange
        var model = "gpt-4";
        object? messages = null;

        // Act
        var result = OpenAIRequestValidator.ValidateRequest(model, messages, null, null);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("Messages are required");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(2.1)]
    [InlineData(-1)]
    [InlineData(3)]
    public void ValidateRequest_WithInvalidTemperature_ReturnsError(double temperature)
    {
        // Arrange
        var model = "gpt-4";
        var messages = new[] { new { role = "user", content = "Hello" } };

        // Act
        var result = OpenAIRequestValidator.ValidateRequest(model, messages, temperature, null);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("Temperature must be between 0 and 2");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ValidateRequest_WithInvalidMaxTokens_ReturnsError(int maxTokens)
    {
        // Arrange
        var model = "gpt-4";
        var messages = new[] { new { role = "user", content = "Hello" } };

        // Act
        var result = OpenAIRequestValidator.ValidateRequest(model, messages, null, maxTokens);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("MaxTokens must be greater than 0");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void ValidateRequest_WithValidTemperature_ReturnsSuccess(double temperature)
    {
        // Arrange
        var model = "gpt-4";
        var messages = new[] { new { role = "user", content = "Hello" } };

        // Act
        var result = OpenAIRequestValidator.ValidateRequest(model, messages, temperature, 100);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(100000)]
    public void ValidateRequest_WithValidMaxTokens_ReturnsSuccess(int maxTokens)
    {
        // Arrange
        var model = "gpt-4";
        var messages = new[] { new { role = "user", content = "Hello" } };

        // Act
        var result = OpenAIRequestValidator.ValidateRequest(model, messages, 0.7, maxTokens);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void IsValidModel_WithValidModel_ReturnsTrue()
    {
        // Arrange
        var model = "gpt-4";

        // Act
        var isValid = OpenAIRequestValidator.IsValidModel(model, out var error);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void IsValidModel_WithNullModel_ReturnsFalse()
    {
        // Arrange
        string? model = null;

        // Act
        var isValid = OpenAIRequestValidator.IsValidModel(model, out var error);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Contain("Model is required");
    }

    [Fact]
    public void IsValidTemperature_WithNullTemperature_ReturnsTrue()
    {
        // Arrange
        double? temperature = null;

        // Act
        var isValid = OpenAIRequestValidator.IsValidTemperature(temperature, out var error);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void IsValidTemperature_WithValidTemperature_ReturnsTrue(double temperature)
    {
        // Arrange & Act
        var isValid = OpenAIRequestValidator.IsValidTemperature(temperature, out var error);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(2.1)]
    public void IsValidTemperature_WithInvalidTemperature_ReturnsFalse(double temperature)
    {
        // Arrange & Act
        var isValid = OpenAIRequestValidator.IsValidTemperature(temperature, out var error);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Contain("Temperature must be between 0 and 2");
    }

    [Fact]
    public void IsValidMaxTokens_WithNullMaxTokens_ReturnsTrue()
    {
        // Arrange
        int? maxTokens = null;

        // Act
        var isValid = OpenAIRequestValidator.IsValidMaxTokens(maxTokens, out var error);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    [InlineData(100000)]
    [InlineData(200000)]
    public void IsValidMaxTokens_WithValidMaxTokens_ReturnsTrue(int maxTokens)
    {
        // Arrange & Act
        var isValid = OpenAIRequestValidator.IsValidMaxTokens(maxTokens, out var error);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IsValidMaxTokens_WithInvalidMaxTokens_ReturnsFalse(int maxTokens)
    {
        // Arrange & Act
        var isValid = OpenAIRequestValidator.IsValidMaxTokens(maxTokens, out var error);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Contain("MaxTokens must be greater than 0");
    }

    [Fact]
    public void IsValidMaxTokens_WithExcessiveMaxTokens_ReturnsFalse()
    {
        // Arrange
        var maxTokens = 200001;

        // Act
        var isValid = OpenAIRequestValidator.IsValidMaxTokens(maxTokens, out var error);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Contain("exceeds maximum allowed value");
    }
}
