namespace Synaxis.Contracts.Tests.V1.Messages;

using FluentAssertions;
using Synaxis.Contracts.V1.Messages;

public class AudioTranscriptionTests
{
    [Fact]
    public void AudioTranscription_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var transcription = new AudioTranscription
        {
            Text = "Hello, this is a test.",
            Duration = 5.5,
            Language = "en",
        };

        // Assert
        transcription.Text.Should().Be("Hello, this is a test.");
        transcription.Duration.Should().Be(5.5);
        transcription.Language.Should().Be("en");
    }

    [Fact]
    public void AudioTranscription_DefaultText_IsEmptyString()
    {
        // Arrange & Act
        var transcription = new AudioTranscription();

        // Assert
        transcription.Text.Should().BeEmpty();
        transcription.Duration.Should().BeNull();
        transcription.Language.Should().BeNull();
    }

    [Fact]
    public void AudioTranscription_WithoutOptionalFields_WorksCorrectly()
    {
        // Arrange & Act
        var transcription = new AudioTranscription
        {
            Text = "Transcribed text",
        };

        // Assert
        transcription.Text.Should().Be("Transcribed text");
        transcription.Duration.Should().BeNull();
        transcription.Language.Should().BeNull();
    }

    [Fact]
    public void AudioTranscription_IsImmutable_CannotBeModifiedAfterCreation()
    {
        // Arrange
        var transcription = new AudioTranscription
        {
            Text = "Sample text",
            Duration = 10.0,
            Language = "fr",
        };

        // Act & Assert
        transcription.Text.Should().Be("Sample text");
        transcription.Duration.Should().Be(10.0);
        transcription.Language.Should().Be("fr");
    }

    [Fact]
    public void AudioTranscription_WithLongText_HandlesMultipleLines()
    {
        // Arrange
        var longText = string.Join("\n", Enumerable.Repeat("This is a line of transcribed text.", 100));

        // Act
        var transcription = new AudioTranscription
        {
            Text = longText,
            Duration = 300.0,
        };

        // Assert
        transcription.Text.Should().Contain("This is a line of transcribed text.");
        transcription.Text.Should().Contain("\n");
        transcription.Duration.Should().Be(300.0);
    }

    [Fact]
    public void AudioTranscription_WithZeroDuration_IsValid()
    {
        // Arrange & Act
        var transcription = new AudioTranscription
        {
            Text = "Quick sound",
            Duration = 0.0,
        };

        // Assert
        transcription.Duration.Should().Be(0.0);
    }
}
