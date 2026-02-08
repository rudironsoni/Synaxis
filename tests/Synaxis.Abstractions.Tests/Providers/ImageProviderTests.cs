namespace Synaxis.Abstractions.Tests.Providers;

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Synaxis.Abstractions.Providers;

public class ImageProviderTests
{
    [Fact]
    public void IImageProvider_ExtendsIProviderClient()
    {
        // Arrange
        var provider = new TestImageProvider();

        // Act & Assert
        provider.Should().BeAssignableTo<IProviderClient>();
        provider.Should().BeAssignableTo<IImageProvider>();
    }

    [Fact]
    public async Task GenerateImageAsync_WithPrompt_ReturnsImage()
    {
        // Arrange
        var provider = new TestImageProvider();
        var prompt = "A beautiful sunset";
        var model = "image-model";

        // Act
        var response = await provider.GenerateImageAsync(prompt, model);

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateImageAsync_WithOptions_AcceptsOptionalParameters()
    {
        // Arrange
        var provider = new TestImageProvider();
        var prompt = "A cat";
        var model = "image-model";
        var options = new { size = "1024x1024", quality = "hd" };

        // Act
        var response = await provider.GenerateImageAsync(prompt, model, options);

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateImageAsync_WithCancellation_PropagatesToken()
    {
        // Arrange
        var provider = new TestImageProvider();
        var prompt = "A dog";
        var model = "image-model";
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        Func<Task> act = async () => await provider.GenerateImageAsync(prompt, model, null, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GenerateImageAsync_WithEmptyPrompt_HandlesEmptyInput()
    {
        // Arrange
        var provider = new TestImageProvider();
        var prompt = string.Empty;
        var model = "image-model";

        // Act
        var response = await provider.GenerateImageAsync(prompt, model);

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateImageAsync_WithLongPrompt_ProcessesLongText()
    {
        // Arrange
        var provider = new TestImageProvider();
        var prompt = new string('A', 1000);
        var model = "image-model";

        // Act
        var response = await provider.GenerateImageAsync(prompt, model);

        // Assert
        response.Should().NotBeNull();
    }

    private sealed class TestImageProvider : IImageProvider
    {
        public string ProviderName => "test-image-provider";

        public Task<object> GenerateImageAsync(
            string prompt,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<object>(new { url = "https://example.com/image.png" });
        }
    }
}
