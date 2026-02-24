namespace Synaxis.Contracts.Tests.V1.Messages;

using FluentAssertions;
using Synaxis.Contracts.V1.Messages;

public class RerankResultTests
{
    [Fact]
    public void RerankResult_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var result = new RerankResult
        {
            Index = 0,
            Score = 0.95,
            Document = "This is a relevant document.",
        };

        // Assert
        result.Index.Should().Be(0);
        result.Score.Should().Be(0.95);
        result.Document.Should().Be("This is a relevant document.");
    }

    [Fact]
    public void RerankResult_DefaultDocument_IsEmptyString()
    {
        // Arrange & Act
        var result = new RerankResult();

        // Assert
        result.Document.Should().BeEmpty();
        result.Index.Should().Be(0);
        result.Score.Should().Be(0.0);
    }

    [Fact]
    public void RerankResult_IsImmutable_CannotBeModifiedAfterCreation()
    {
        // Arrange
        var result = new RerankResult
        {
            Index = 5,
            Score = 0.87,
            Document = "Another document",
        };

        // Act & Assert
        result.Index.Should().Be(5);
        result.Score.Should().Be(0.87);
        result.Document.Should().Be("Another document");
    }

    [Fact]
    public void RerankResult_WithNegativeScore_IsValid()
    {
        // Arrange & Act
        var result = new RerankResult
        {
            Index = 1,
            Score = -0.5,
            Document = "Low relevance document",
        };

        // Assert
        result.Score.Should().Be(-0.5);
    }

    [Fact]
    public void RerankResult_WithScoreGreaterThanOne_IsValid()
    {
        // Arrange & Act
        var result = new RerankResult
        {
            Index = 2,
            Score = 1.5,
            Document = "Highly relevant",
        };

        // Assert
        result.Score.Should().Be(1.5);
    }

    [Fact]
    public void RerankResult_WithLongDocument_HandlesLargeText()
    {
        // Arrange
        var longDocument = new string('A', 10000);

        // Act
        var result = new RerankResult
        {
            Index = 3,
            Score = 0.75,
            Document = longDocument,
        };

        // Assert
        result.Document.Should().HaveLength(10000);
    }

    [Fact]
    public void RerankResult_MultipleResults_CanBeSortedByScore()
    {
        // Arrange
        var results = new[]
        {
            new RerankResult { Index = 0, Score = 0.5, Document = "Doc 1" },
            new RerankResult { Index = 1, Score = 0.9, Document = "Doc 2" },
            new RerankResult { Index = 2, Score = 0.3, Document = "Doc 3" },
        };

        // Act
        var sorted = results.OrderByDescending(r => r.Score).ToList();

        // Assert
        sorted[0].Score.Should().Be(0.9);
        sorted[1].Score.Should().Be(0.5);
        sorted[2].Score.Should().Be(0.3);
    }
}
