using ContextSavvy.LlmProviders.Application.Dtos;
using FluentAssertions;

namespace ContextSavvy.LlmProviders.Application.Tests.Dtos;

public class DtoTests
{
    [Fact]
    public void ChatCompletionResult_ShouldSetProperties()
    {
        var result = new ChatCompletionResult("content", "model", 100);
        result.Content.Should().Be("content");
        result.Model.Should().Be("model");
        result.UsageTokens.Should().Be(100);
    }

    [Fact]
    public void ChatMessage_ShouldSetProperties()
    {
        var message = new ChatMessage("user", "hello");
        message.Role.Should().Be("user");
        message.Content.Should().Be("hello");
    }

    [Fact]
    public void ModelInfo_ShouldSetProperties()
    {
        var model = new ModelInfo("id", "name", "provider", 1024);
        model.Id.Should().Be("id");
        model.Name.Should().Be("name");
        model.Provider.Should().Be("provider");
        model.ContextWindow.Should().Be(1024);
    }

    [Fact]
    public void ProviderStatusDto_ShouldSetProperties()
    {
        var now = DateTime.UtcNow;
        var status = new ProviderStatusDto("provider", true, "message", now);
        status.ProviderName.Should().Be("provider");
        status.IsHealthy.Should().BeTrue();
        status.StatusMessage.Should().Be("message");
        status.LastChecked.Should().Be(now);
    }
}
