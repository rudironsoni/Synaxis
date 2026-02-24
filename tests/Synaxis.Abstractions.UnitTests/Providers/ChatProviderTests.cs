namespace Synaxis.Abstractions.Tests.Providers;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Synaxis.Abstractions.Providers;

public class ChatProviderTests
{
    [Fact]
    public void IChatProvider_ExtendsIProviderClient()
    {
        // Arrange
        var provider = new TestChatProvider();

        // Act & Assert
        provider.Should().BeAssignableTo<IProviderClient>();
        provider.Should().BeAssignableTo<IChatProvider>();
    }

    [Fact]
    public async Task ChatAsync_WithMessages_ReturnsResponse()
    {
        // Arrange
        var provider = new TestChatProvider();
        var messages = new List<object> { new { role = "user", content = "Hello" } };
        var model = "test-model";

        // Act
        var response = await provider.ChatAsync(messages, model);

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task ChatAsync_WithOptions_AcceptsOptionalParameters()
    {
        // Arrange
        var provider = new TestChatProvider();
        var messages = new List<object> { new { role = "user", content = "Hello" } };
        var model = "test-model";
        var options = new { temperature = 0.7 };

        // Act
        var response = await provider.ChatAsync(messages, model, options);

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task ChatAsync_WithCancellation_PropagatesToken()
    {
        // Arrange
        var provider = new TestChatProvider();
        var messages = new List<object> { new { role = "user", content = "Hello" } };
        var model = "test-model";
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        Func<Task> act = async () => await provider.ChatAsync(messages, model, null, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ChatAsync_WithEmptyMessages_HandlesEmptyInput()
    {
        // Arrange
        var provider = new TestChatProvider();
        var messages = Enumerable.Empty<object>();
        var model = "test-model";

        // Act
        var response = await provider.ChatAsync(messages, model);

        // Assert
        response.Should().NotBeNull();
    }

    private sealed class TestChatProvider : IChatProvider
    {
        public string ProviderName => "test-chat-provider";

        public Task<object> ChatAsync(
            IEnumerable<object> messages,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<object>(new { content = "response" });
        }
    }
}
