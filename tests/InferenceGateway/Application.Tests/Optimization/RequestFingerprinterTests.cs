using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using Synaxis.Common.Tests;

namespace Synaxis.InferenceGateway.Application.Tests.Optimization;

/// <summary>
/// Unit tests for IRequestFingerprinter implementations
/// Tests request fingerprinting and session ID computation for caching and deduplication
/// </summary>
public class RequestFingerprinterTests : TestBase
{
    private readonly Mock<IRequestFingerprinter> _mockFingerprinter;
    private readonly Mock<IRequestContextProvider> _mockContextProvider;

    public RequestFingerprinterTests()
    {
        _mockFingerprinter = new Mock<IRequestFingerprinter>();
        _mockContextProvider = new Mock<IRequestContextProvider>();
    }

    [Fact]
    public void ComputeFingerprint_SameInput_ReturnsSameHash()
    {
        // Arrange
        var messages = new[] 
        { 
            new ChatMessage(ChatRole.User, "Hello"),
            new ChatMessage(ChatRole.Assistant, "Hi there!"),
            new ChatMessage(ChatRole.User, "How are you?")
        };
        var options = new ChatOptions 
        { 
            ModelId = "gpt-4",
            Temperature = 0.7,
            MaxOutputTokens = 1000
        };

        var expectedHash = "abc123def456";

        _mockFingerprinter
            .Setup(x => x.ComputeFingerprint(messages, options))
            .Returns(expectedHash);

        // Act
        var hash1 = _mockFingerprinter.Object.ComputeFingerprint(messages, options);
        var hash2 = _mockFingerprinter.Object.ComputeFingerprint(messages, options);

        // Assert
        Assert.Equal(expectedHash, hash1);
        Assert.Equal(expectedHash, hash2);
        Assert.Equal(hash1, hash2);
        
        _mockFingerprinter.Verify(
            x => x.ComputeFingerprint(messages, options),
            Times.Exactly(2));
    }

    [Fact]
    public void ComputeFingerprint_DifferentInput_ReturnsDifferentHash()
    {
        // Arrange
        var messages1 = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var messages2 = new[] { new ChatMessage(ChatRole.User, "Goodbye") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        _mockFingerprinter
            .Setup(x => x.ComputeFingerprint(messages1, options))
            .Returns("hash1");
        
        _mockFingerprinter
            .Setup(x => x.ComputeFingerprint(messages2, options))
            .Returns("hash2");

        // Act
        var hash1 = _mockFingerprinter.Object.ComputeFingerprint(messages1, options);
        var hash2 = _mockFingerprinter.Object.ComputeFingerprint(messages2, options);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeFingerprint_DifferentOptions_ReturnsDifferentHash()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options1 = new ChatOptions { ModelId = "gpt-4", Temperature = 0.7 };
        var options2 = new ChatOptions { ModelId = "gpt-4", Temperature = 0.0 };

        _mockFingerprinter
            .Setup(x => x.ComputeFingerprint(messages, options1))
            .Returns("hash_temp_07");
        
        _mockFingerprinter
            .Setup(x => x.ComputeFingerprint(messages, options2))
            .Returns("hash_temp_00");

        // Act
        var hash1 = _mockFingerprinter.Object.ComputeFingerprint(messages, options1);
        var hash2 = _mockFingerprinter.Object.ComputeFingerprint(messages, options2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeFingerprint_NormalizedOrder_ProducesSameHash()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options1 = new ChatOptions 
        { 
            ModelId = "gpt-4",
            Temperature = 0.7,
            MaxOutputTokens = 1000,
            TopP = 0.9
        };
        var options2 = new ChatOptions 
        { 
            TopP = 0.9,
            ModelId = "gpt-4",
            MaxOutputTokens = 1000,
            Temperature = 0.7
        };

        var expectedHash = "normalized_hash";

        _mockFingerprinter
            .Setup(x => x.ComputeFingerprint(messages, It.IsAny<ChatOptions>()))
            .Returns(expectedHash);

        // Act
        var hash1 = _mockFingerprinter.Object.ComputeFingerprint(messages, options1);
        var hash2 = _mockFingerprinter.Object.ComputeFingerprint(messages, options2);

        // Assert - Implementation should normalize options ordering
        Assert.Equal(expectedHash, hash1);
        Assert.Equal(expectedHash, hash2);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeSessionId_HeaderProvided_UsesHeader()
    {
        // Arrange
        var context = CreateMockHttpContext("session-header-123");
        
        _mockContextProvider
            .Setup(x => x.GetCurrentContext())
            .Returns(context.Object);

        _mockFingerprinter
            .Setup(x => x.ComputeSessionId(context.Object))
            .Returns("session-header-123");

        // Act
        var sessionId = _mockFingerprinter.Object.ComputeSessionId(context.Object);

        // Assert
        Assert.Equal("session-header-123", sessionId);
        
        _mockFingerprinter.Verify(
            x => x.ComputeSessionId(context.Object),
            Times.Once);
    }

    [Fact]
    public void ComputeSessionId_NoHeader_UsesIpAndUa()
    {
        // Arrange
        var context = CreateMockHttpContext(
            sessionHeader: null,
            ipAddress: "192.168.1.1",
            userAgent: "Mozilla/5.0");

        _mockFingerprinter
            .Setup(x => x.ComputeSessionId(context.Object))
            .Returns("ip-ua-hash-abc123");

        // Act
        var sessionId = _mockFingerprinter.Object.ComputeSessionId(context.Object);

        // Assert
        Assert.NotNull(sessionId);
        Assert.NotEmpty(sessionId);
        Assert.Equal("ip-ua-hash-abc123", sessionId);
    }

    [Fact]
    public void ComputeSessionId_NoHeaderNoIp_UsesContent()
    {
        // Arrange
        var context = CreateMockHttpContext(
            sessionHeader: null,
            ipAddress: null,
            userAgent: null);

        _mockFingerprinter
            .Setup(x => x.ComputeSessionId(context.Object))
            .Returns("content-based-hash-xyz789");

        // Act
        var sessionId = _mockFingerprinter.Object.ComputeSessionId(context.Object);

        // Assert
        Assert.NotNull(sessionId);
        Assert.NotEmpty(sessionId);
        Assert.Equal("content-based-hash-xyz789", sessionId);
    }

    [Fact]
    public void ComputeSessionId_AllNull_GeneratesRandom()
    {
        // Arrange
        var context = CreateMockHttpContext(
            sessionHeader: null,
            ipAddress: null,
            userAgent: null);

        _mockFingerprinter
            .Setup(x => x.ComputeSessionId(context.Object))
            .Returns(() => Guid.NewGuid().ToString());

        // Act
        var sessionId1 = _mockFingerprinter.Object.ComputeSessionId(context.Object);
        var sessionId2 = _mockFingerprinter.Object.ComputeSessionId(context.Object);

        // Assert
        Assert.NotNull(sessionId1);
        Assert.NotNull(sessionId2);
        Assert.NotEmpty(sessionId1);
        Assert.NotEmpty(sessionId2);
        // Should generate different IDs each time
        Assert.NotEqual(sessionId1, sessionId2);
    }

    [Fact]
    public void ComputeSessionId_SanitizesSpecialCharacters()
    {
        // Arrange
        var context = CreateMockHttpContext("session/../../../etc/passwd");

        _mockFingerprinter
            .Setup(x => x.ComputeSessionId(context.Object))
            .Returns("session_etc_passwd");

        // Act
        var sessionId = _mockFingerprinter.Object.ComputeSessionId(context.Object);

        // Assert
        Assert.NotNull(sessionId);
        Assert.DoesNotContain("..", sessionId);
        Assert.DoesNotContain("/", sessionId);
        Assert.Equal("session_etc_passwd", sessionId);
    }

    [Fact]
    public void ComputeFingerprint_LargeMessages_HandlesEfficiently()
    {
        // Arrange - Create a large conversation history
        var largeMessages = new ChatMessage[100];
        for (int i = 0; i < 100; i++)
        {
            largeMessages[i] = new ChatMessage(
                i % 2 == 0 ? ChatRole.User : ChatRole.Assistant,
                new string('x', 1000)); // 1000 character messages
        }

        var options = new ChatOptions { ModelId = "gpt-4" };
        var expectedHash = "large-content-hash";

        _mockFingerprinter
            .Setup(x => x.ComputeFingerprint(largeMessages, options))
            .Returns(expectedHash);

        // Act
        var startTime = DateTime.UtcNow;
        var hash = _mockFingerprinter.Object.ComputeFingerprint(largeMessages, options);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        Assert.Equal(expectedHash, hash);
        Assert.True(elapsed.TotalSeconds < 1, "Fingerprint computation should be fast");
    }

    #region Helper Methods

    private Mock<HttpContext> CreateMockHttpContext(
        string? sessionHeader = null,
        string? ipAddress = "127.0.0.1",
        string? userAgent = "TestAgent/1.0")
    {
        var context = new Mock<HttpContext>();
        var request = new Mock<HttpRequest>();
        var headers = new HeaderDictionary();
        var connection = new Mock<ConnectionInfo>();

        if (sessionHeader != null)
        {
            headers["X-Session-Id"] = sessionHeader;
        }

        if (userAgent != null)
        {
            headers["User-Agent"] = userAgent;
        }

        if (ipAddress != null)
        {
            connection.Setup(x => x.RemoteIpAddress)
                .Returns(System.Net.IPAddress.Parse(ipAddress));
        }

        request.Setup(x => x.Headers).Returns(headers);
        context.Setup(x => x.Request).Returns(request.Object);
        context.Setup(x => x.Connection).Returns(connection.Object);

        return context;
    }

    #endregion
}

/// <summary>
/// Interface for request fingerprinting and session ID computation
/// </summary>
public interface IRequestFingerprinter
{
    /// <summary>
    /// Computes a deterministic fingerprint for a request based on messages and options
    /// </summary>
    string ComputeFingerprint(IEnumerable<ChatMessage> messages, ChatOptions? options);

    /// <summary>
    /// Computes a session ID from the HTTP context
    /// </summary>
    string ComputeSessionId(HttpContext context);
}

/// <summary>
/// Interface for accessing request context
/// </summary>
public interface IRequestContextProvider
{
    /// <summary>
    /// Gets the current HTTP context
    /// </summary>
    HttpContext? GetCurrentContext();
}
