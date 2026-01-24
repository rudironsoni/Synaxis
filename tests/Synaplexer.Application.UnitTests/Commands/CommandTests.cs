using Synaplexer.Application.Commands;
using Synaplexer.Application.Dtos;
using FluentAssertions;

namespace Synaplexer.Application.Tests.Commands;

public class CommandTests
{
    [Fact]
    public void ChatCompletionCommand_ShouldSetProperties()
    {
        var messages = new[] { new ChatMessage("user", "hello") };
        var command = new ChatCompletionCommand("model", messages, 0.5f, 100, true);
        
        command.Model.Should().Be("model");
        command.Messages.Should().BeSameAs(messages);
        command.Temperature.Should().Be(0.5f);
        command.MaxTokens.Should().Be(100);
        command.Stream.Should().BeTrue();
    }

    [Fact]
    public void InitializeProviderCommand_ShouldSetProperties()
    {
        var command = new InitializeProviderCommand("type", true);
        command.ProviderType.Should().Be("type");
        command.ForceReinitialize.Should().BeTrue();
    }
}
