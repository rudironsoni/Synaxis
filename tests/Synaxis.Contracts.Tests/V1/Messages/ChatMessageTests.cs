namespace Synaxis.Contracts.Tests.V1.Messages;

using FluentAssertions;
using Synaxis.Contracts.V1.Messages;

public class ChatMessageTests
{
    [Fact]
    public void ChatMessage_CanBeCreatedWithInitProperties()
    {
        // Arrange & Act
        var message = new ChatMessage
        {
            Role = "user",
            Content = "Hello",
            Name = "John",
        };

        // Assert
        message.Role.Should().Be("user");
        message.Content.Should().Be("Hello");
        message.Name.Should().Be("John");
    }

    [Fact]
    public void ChatMessage_DefaultValues_AreEmptyStrings()
    {
        // Arrange & Act
        var message = new ChatMessage();

        // Assert
        message.Role.Should().BeEmpty();
        message.Content.Should().BeEmpty();
        message.Name.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_IsImmutable_CannotBeModifiedAfterCreation()
    {
        // Arrange
        var message = new ChatMessage
        {
            Role = "assistant",
            Content = "Response",
        };

        // Act & Assert
        // Properties are init-only, attempting to set them after construction would not compile
        message.Role.Should().Be("assistant");
        message.Content.Should().Be("Response");
    }

    [Fact]
    public void ChatMessage_WithoutName_WorksCorrectly()
    {
        // Arrange & Act
        var message = new ChatMessage
        {
            Role = "system",
            Content = "You are a helpful assistant.",
        };

        // Assert
        message.Role.Should().Be("system");
        message.Content.Should().Be("You are a helpful assistant.");
        message.Name.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_WithComplexContent_HandlesMultilineText()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3";

        // Act
        var message = new ChatMessage
        {
            Role = "user",
            Content = content,
        };

        // Assert
        message.Content.Should().Be(content);
        message.Content.Should().Contain("\n");
    }
}
