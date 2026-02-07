using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Application.ChatClients;

namespace Synaxis.InferenceGateway.Application.Tests;

public class UsageTrackingTests
{
    private readonly Mock<IChatClient> _innerMock;
    private readonly Mock<ILogger<UsageTrackingChatClient>> _loggerMock;
    private readonly UsageTrackingChatClient _client;

    public UsageTrackingTests()
    {
        this._innerMock = new Mock<IChatClient>();
        this._loggerMock = new Mock<ILogger<UsageTrackingChatClient>>();
        this._client = new UsageTrackingChatClient(this._innerMock.Object, this._loggerMock.Object);
    }

    [Fact]
    public async Task CompleteAsync_LogsUsage()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "llama3-70b" };
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Response"))
        {
            ModelId = "llama3-70b",
            Usage = new UsageDetails
            {
                InputTokenCount = 100,
                OutputTokenCount = 200
            },
        };

        this._innerMock.Setup(c => c.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await this._client.GetResponseAsync(messages, options);

        // Assert
        Assert.Equal(expectedResponse, response);

        // Verify logger was called with correct cost
        // llama cost: 0.70/1M input, 0.80/1M output
        // (100 * 0.70 / 1,000,000) + (200 * 0.80 / 1,000,000) = 0.00007 + 0.00016 = 0.00023
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("0.000230")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_DelegatesToInner()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var update = new ChatResponseUpdate();
        update.Contents.Add(new TextContent("Part 1"));
        var updates = new[] { update }.ToAsyncEnumerable();

        this._innerMock.Setup(c => c.GetStreamingResponseAsync(messages, null, It.IsAny<CancellationToken>()))
            .Returns(updates);

        // Act
        var response = this._client.GetStreamingResponseAsync(messages);
        var result = await response.ToListAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Part 1", result[0].Text);
        this._innerMock.Verify(c => c.GetStreamingResponseAsync(messages, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
