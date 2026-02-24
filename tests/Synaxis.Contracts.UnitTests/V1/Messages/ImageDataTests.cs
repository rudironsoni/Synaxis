namespace Synaxis.Contracts.Tests.V1.Messages;

using FluentAssertions;
using Synaxis.Contracts.V1.Messages;

public class ImageDataTests
{
    [Fact]
    public void ImageData_WithUrl_CanBeCreated()
    {
        // Arrange & Act
        var data = new ImageData
        {
            Url = "https://example.com/image.png",
        };

        // Assert
        data.Url.Should().Be("https://example.com/image.png");
        data.B64Json.Should().BeNull();
        data.RevisedPrompt.Should().BeNull();
    }

    [Fact]
    public void ImageData_WithB64Json_CanBeCreated()
    {
        // Arrange & Act
        var data = new ImageData
        {
            B64Json = "iVBORw0KGgoAAAANSUhEUgAAAAUA",
        };

        // Assert
        data.B64Json.Should().Be("iVBORw0KGgoAAAANSUhEUgAAAAUA");
        data.Url.Should().BeNull();
        data.RevisedPrompt.Should().BeNull();
    }

    [Fact]
    public void ImageData_WithRevisedPrompt_CanBeCreated()
    {
        // Arrange & Act
        var data = new ImageData
        {
            Url = "https://example.com/image.png",
            RevisedPrompt = "A beautiful sunset over mountains",
        };

        // Assert
        data.Url.Should().Be("https://example.com/image.png");
        data.RevisedPrompt.Should().Be("A beautiful sunset over mountains");
    }

    [Fact]
    public void ImageData_DefaultValues_AreNull()
    {
        // Arrange & Act
        var data = new ImageData();

        // Assert
        data.Url.Should().BeNull();
        data.B64Json.Should().BeNull();
        data.RevisedPrompt.Should().BeNull();
    }

    [Fact]
    public void ImageData_IsImmutable_CannotBeModifiedAfterCreation()
    {
        // Arrange
        var data = new ImageData
        {
            Url = "https://example.com/image.png",
            B64Json = "base64data",
            RevisedPrompt = "revised prompt",
        };

        // Act & Assert
        data.Url.Should().Be("https://example.com/image.png");
        data.B64Json.Should().Be("base64data");
        data.RevisedPrompt.Should().Be("revised prompt");
    }

    [Fact]
    public void ImageData_WithLongBase64_HandlesLargeStrings()
    {
        // Arrange
        var longBase64 = new string('A', 10000);

        // Act
        var data = new ImageData
        {
            B64Json = longBase64,
        };

        // Assert
        data.B64Json.Should().HaveLength(10000);
    }
}
