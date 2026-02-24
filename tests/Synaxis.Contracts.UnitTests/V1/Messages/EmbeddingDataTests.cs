namespace Synaxis.Contracts.Tests.V1.Messages;

using FluentAssertions;
using Synaxis.Contracts.V1.Messages;

public class EmbeddingDataTests
{
    [Fact]
    public void EmbeddingData_CanBeCreatedWithAllProperties()
    {
        // Arrange
        var embedding = new[] { 0.1f, 0.2f, 0.3f };

        // Act
        var data = new EmbeddingData
        {
            Index = 0,
            Embedding = embedding,
            Object = "embedding",
        };

        // Assert
        data.Index.Should().Be(0);
        data.Embedding.Should().Equal(embedding);
        data.Object.Should().Be("embedding");
    }

    [Fact]
    public void EmbeddingData_DefaultObject_IsEmbedding()
    {
        // Arrange & Act
        var data = new EmbeddingData
        {
            Index = 1,
            Embedding = new[] { 0.5f },
        };

        // Assert
        data.Object.Should().Be("embedding");
    }

    [Fact]
    public void EmbeddingData_DefaultEmbedding_IsEmptyArray()
    {
        // Arrange & Act
        var data = new EmbeddingData();

        // Assert
        data.Embedding.Should().BeEmpty();
    }

    [Fact]
    public void EmbeddingData_IsImmutable_CannotBeModifiedAfterCreation()
    {
        // Arrange
        var data = new EmbeddingData
        {
            Index = 2,
            Embedding = new[] { 0.1f, 0.2f },
        };

        // Act & Assert
        data.Index.Should().Be(2);
        data.Embedding.Length.Should().Be(2);
    }

    [Fact]
    public void EmbeddingData_WithLargeVector_HandlesHighDimensions()
    {
        // Arrange
        var largeEmbedding = new float[1536];
        for (var i = 0; i < largeEmbedding.Length; i++)
        {
            largeEmbedding[i] = i * 0.001f;
        }

        // Act
        var data = new EmbeddingData
        {
            Index = 0,
            Embedding = largeEmbedding,
        };

        // Assert
        data.Embedding.Length.Should().Be(1536);
        data.Embedding[0].Should().Be(0f);
        data.Embedding[1535].Should().BeApproximately(1.535f, 0.0001f);
    }
}
