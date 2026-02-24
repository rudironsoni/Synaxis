namespace Synaxis.Abstractions.Tests.Providers;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Synaxis.Abstractions.Providers;

public class EmbeddingProviderTests
{
    [Fact]
    public void IEmbeddingProvider_ExtendsIProviderClient()
    {
        // Arrange
        var provider = new TestEmbeddingProvider();

        // Act & Assert
        provider.Should().BeAssignableTo<IProviderClient>();
        provider.Should().BeAssignableTo<IEmbeddingProvider>();
    }

    [Fact]
    public async Task EmbedAsync_WithInputs_ReturnsEmbeddings()
    {
        // Arrange
        var provider = new TestEmbeddingProvider();
        var inputs = new List<string> { "text1", "text2" };
        var model = "embedding-model";

        // Act
        var response = await provider.EmbedAsync(inputs, model);

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task EmbedAsync_WithOptions_AcceptsOptionalParameters()
    {
        // Arrange
        var provider = new TestEmbeddingProvider();
        var inputs = new List<string> { "text" };
        var model = "embedding-model";
        var options = new { dimensions = 768 };

        // Act
        var response = await provider.EmbedAsync(inputs, model, options);

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task EmbedAsync_WithCancellation_PropagatesToken()
    {
        // Arrange
        var provider = new TestEmbeddingProvider();
        var inputs = new List<string> { "text" };
        var model = "embedding-model";
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        Func<Task> act = async () => await provider.EmbedAsync(inputs, model, null, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task EmbedAsync_WithEmptyInputs_HandlesEmptyInput()
    {
        // Arrange
        var provider = new TestEmbeddingProvider();
        var inputs = Enumerable.Empty<string>();
        var model = "embedding-model";

        // Act
        var response = await provider.EmbedAsync(inputs, model);

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task EmbedAsync_WithMultipleInputs_ProcessesBatch()
    {
        // Arrange
        var provider = new TestEmbeddingProvider();
        var inputs = new List<string> { "text1", "text2", "text3" };
        var model = "embedding-model";

        // Act
        var response = await provider.EmbedAsync(inputs, model);

        // Assert
        response.Should().NotBeNull();
    }

    private sealed class TestEmbeddingProvider : IEmbeddingProvider
    {
        public string ProviderName => "test-embedding-provider";

        public Task<object> EmbedAsync(
            IEnumerable<string> inputs,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<object>(new { embeddings = new[] { new[] { 0.1, 0.2 } } });
        }
    }
}
