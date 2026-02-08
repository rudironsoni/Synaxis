namespace Synaxis.Contracts.Tests.V1.Messages;

using FluentAssertions;
using Synaxis.Contracts.V1.Messages;

public class AudioSynthesisTests
{
    [Fact]
    public void AudioSynthesis_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var synthesis = new AudioSynthesis
        {
            AudioData = new byte[] { 0x01, 0x02, 0x03 },
            ContentType = "audio/mpeg",
        };

        // Assert
        synthesis.AudioData.Should().Equal(0x01, 0x02, 0x03);
        synthesis.ContentType.Should().Be("audio/mpeg");
    }

    [Fact]
    public void AudioSynthesis_DefaultValues_AreSet()
    {
        // Arrange & Act
        var synthesis = new AudioSynthesis();

        // Assert
        synthesis.AudioData.Should().BeEmpty();
        synthesis.ContentType.Should().Be("audio/mpeg");
    }

    [Fact]
    public void AudioSynthesis_WithWavFormat_CanBeCreated()
    {
        // Arrange & Act
        var synthesis = new AudioSynthesis
        {
            AudioData = new byte[] { 0xFF },
            ContentType = "audio/wav",
        };

        // Assert
        synthesis.AudioData.Should().Equal(0xFF);
        synthesis.ContentType.Should().Be("audio/wav");
    }

    [Fact]
    public void AudioSynthesis_IsImmutable_CannotBeModifiedAfterCreation()
    {
        // Arrange
        var synthesis = new AudioSynthesis
        {
            AudioData = new byte[] { 0xAA, 0xBB },
            ContentType = "audio/ogg",
        };

        // Act & Assert
        synthesis.AudioData.Should().Equal(0xAA, 0xBB);
        synthesis.ContentType.Should().Be("audio/ogg");
    }

    [Fact]
    public void AudioSynthesis_WithLargeAudioData_HandlesLargeArrays()
    {
        // Arrange
        var largeData = new byte[100000];
        for (var i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }

        // Act
        var synthesis = new AudioSynthesis
        {
            AudioData = largeData,
            ContentType = "audio/mpeg",
        };

        // Assert
        synthesis.AudioData.Should().HaveCount(100000);
        synthesis.AudioData[0].Should().Be(0);
        synthesis.AudioData[99999].Should().Be(159);
    }

    [Theory]
    [InlineData("audio/mpeg")]
    [InlineData("audio/wav")]
    [InlineData("audio/ogg")]
    [InlineData("audio/flac")]
    public void AudioSynthesis_WithDifferentContentTypes_CanBeCreated(string contentType)
    {
        // Arrange & Act
        var synthesis = new AudioSynthesis
        {
            AudioData = new byte[] { 0x00 },
            ContentType = contentType,
        };

        // Assert
        synthesis.ContentType.Should().Be(contentType);
    }

    [Fact]
    public void AudioSynthesis_EmptyAudioData_IsValid()
    {
        // Arrange & Act
        var synthesis = new AudioSynthesis
        {
            ContentType = "audio/mpeg",
        };

        // Assert
        synthesis.AudioData.Should().BeEmpty();
        synthesis.AudioData.Should().NotBeNull();
    }
}
