using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Application.Translation;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.WebApi.Agents;
using Synaxis.InferenceGateway.WebApi.Middleware;
using System.Runtime.CompilerServices;
using AgentsAI = Microsoft.Agents.AI;

namespace Synaxis.InferenceGateway.Application.Tests.Routing;

public class RoutingServiceTests
{
    private readonly Mock<IChatClient> _chatClientMock;
    private readonly Mock<ITranslationPipeline> _translatorMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<RoutingService>> _loggerMock;
    private readonly DefaultHttpContext _httpContext;

    public RoutingServiceTests()
    {
        this._chatClientMock = new Mock<IChatClient>();
        this._translatorMock = new Mock<ITranslationPipeline>();
        this._httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        this._loggerMock = new Mock<ILogger<RoutingService>>();
        this._httpContext = new DefaultHttpContext();

        this._httpContextAccessorMock.Setup(x => x.HttpContext).Returns(this._httpContext);
    }

    private RoutingService CreateService()
    {
        return new RoutingService(
            this._chatClientMock.Object,
            this._translatorMock.Object,
            this._httpContextAccessorMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        var service = this.CreateService();

        Assert.NotNull(service);
    }

    #endregion

    #region HandleAsync Tests

    [Fact]
    public async Task HandleAsync_WithValidRequest_ReturnsAgentResponse()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "gpt-4",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };

        var canonicalRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello! How can I help you?"));
        var canonicalResponse = new CanonicalResponse("Hello! How can I help you?");
        var translatedResponse = new CanonicalResponse("Hello! How can I help you?");

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._chatClientMock.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        this._translatorMock.Setup(x => x.TranslateResponse(It.IsAny<CanonicalResponse>())).Returns(translatedResponse);

        var service = this.CreateService();

        var result = await service.HandleAsync(openAIRequest, messages);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task HandleAsync_WithNullRequest_ThrowsNullReferenceException()
    {
        OpenAIRequest openAIRequest = null!;
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };
        var service = this.CreateService();

        await Assert.ThrowsAsync<NullReferenceException>(() => service.HandleAsync(openAIRequest, messages));
    }

    [Fact]
    public async Task HandleAsync_WithEmptyMessages_ReturnsResponse()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "gpt-4",
            Messages = new List<OpenAIMessage>(),
        };
        var messages = new List<ChatMessage>();

        var canonicalRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "I received an empty message."));
        var translatedResponse = new CanonicalResponse("I received an empty message.");

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._chatClientMock.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        this._translatorMock.Setup(x => x.TranslateResponse(It.IsAny<CanonicalResponse>())).Returns(translatedResponse);

        var service = this.CreateService();

        var result = await service.HandleAsync(openAIRequest, messages);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task HandleAsync_WithToolCalls_ReturnsResponseWithToolCalls()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "gpt-4",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "What's the weather?" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "What's the weather?") };

        var toolCalls = new List<FunctionCallContent>
        {
            new FunctionCallContent("call-123", "get_weather", new Dictionary<string, object?> { ["location"] = "New York" }),
        };
        var assistantMessage = new ChatMessage(ChatRole.Assistant, "");
        foreach (var toolCall in toolCalls)
        {
            assistantMessage.Contents.Add(toolCall);
        }
        var chatResponse = new ChatResponse(assistantMessage);

        var canonicalRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var canonicalResponse = new CanonicalResponse("", toolCalls);
        var translatedResponse = new CanonicalResponse("", toolCalls);

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._chatClientMock.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        this._translatorMock.Setup(x => x.TranslateResponse(It.IsAny<CanonicalResponse>())).Returns(translatedResponse);

        var service = this.CreateService();

        var result = await service.HandleAsync(openAIRequest, messages);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task HandleAsync_WithNullHttpContext_DoesNotThrow()
    {
        this._httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var openAIRequest = new OpenAIRequest
        {
            Model = "gpt-4",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello!"));
        var translatedResponse = new CanonicalResponse("Hello!");

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._chatClientMock.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        this._translatorMock.Setup(x => x.TranslateResponse(It.IsAny<CanonicalResponse>())).Returns(translatedResponse);

        var service = this.CreateService();

        var result = await service.HandleAsync(openAIRequest, messages);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task HandleAsync_WithResponseAdditionalProperties_SetsRoutingContext()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "gpt-4",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello!"));
        chatResponse.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            ["model_id"] = "gpt-4-turbo",
            ["provider_name"] = "openai",
        };

        var translatedResponse = new CanonicalResponse("Hello!");

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._chatClientMock.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        this._translatorMock.Setup(x => x.TranslateResponse(It.IsAny<CanonicalResponse>())).Returns(translatedResponse);

        var service = this.CreateService();

        await service.HandleAsync(openAIRequest, messages);

        Assert.True(this._httpContext.Items.ContainsKey("RoutingContext"));
        var routingContext = this._httpContext.Items["RoutingContext"] as RoutingContext;
        Assert.NotNull(routingContext);
        Assert.Equal("gpt-4", routingContext.RequestedModel);
        Assert.Equal("gpt-4-turbo", routingContext.ResolvedCanonicalId);
        Assert.Equal("openai", routingContext.Provider);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyResponseMessage_ReturnsEmptyContent()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "gpt-4",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, ""));
        var translatedResponse = new CanonicalResponse("");

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._chatClientMock.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        this._translatorMock.Setup(x => x.TranslateResponse(It.IsAny<CanonicalResponse>())).Returns(translatedResponse);

        var service = this.CreateService();

        var result = await service.HandleAsync(openAIRequest, messages);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task HandleAsync_PassesCorrectModelIdToChatOptions()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "custom-model",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "translated-model", messages);
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello!"));
        var translatedResponse = new CanonicalResponse("Hello!");

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._chatClientMock.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        this._translatorMock.Setup(x => x.TranslateResponse(It.IsAny<CanonicalResponse>())).Returns(translatedResponse);

        var service = this.CreateService();

        await service.HandleAsync(openAIRequest, messages);

        this._chatClientMock.Verify(x => x.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.Is<ChatOptions>(o => o.ModelId == "translated-model"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithCancellationToken_PassesTokenToChatClient()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "gpt-4",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };
        var cancellationToken = new CancellationToken(true);

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello!"));
        var translatedResponse = new CanonicalResponse("Hello!");

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._chatClientMock.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), cancellationToken))
            .ReturnsAsync(chatResponse);
        this._translatorMock.Setup(x => x.TranslateResponse(It.IsAny<CanonicalResponse>())).Returns(translatedResponse);

        var service = this.CreateService();

        await service.HandleAsync(openAIRequest, messages, cancellationToken: cancellationToken);

        this._chatClientMock.Verify(x => x.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            cancellationToken),
            Times.Once);
    }

    #endregion

    #region HandleStreamingAsync Tests

    [Fact]
    public async Task HandleStreamingAsync_WithValidRequest_ReturnsStreamingUpdates()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "gpt-4",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var updates = new List<ChatResponseUpdate>
        {
            new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = { new TextContent("Hello") } },
            new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = { new TextContent("!") } },
        };

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._translatorMock.Setup(x => x.TranslateUpdate(It.IsAny<ChatResponseUpdate>()))
            .Returns<ChatResponseUpdate>(u => u);
        this._chatClientMock.Setup(x => x.GetStreamingResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .Returns(updates.ToAsyncEnumerable());

        var service = this.CreateService();

        var results = new List<AgentsAI.AgentResponseUpdate>();
        await foreach (var update in service.HandleStreamingAsync(openAIRequest, messages))
        {
            results.Add(update);
        }

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task HandleStreamingAsync_WithNullRequest_ThrowsNullReferenceException()
    {
        OpenAIRequest openAIRequest = null!;
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };
        var service = this.CreateService();

        await Assert.ThrowsAsync<NullReferenceException>(async () =>
        {
            await foreach (var _ in service.HandleStreamingAsync(openAIRequest, messages)) { }
        });
    }

    [Fact]
    public async Task HandleStreamingAsync_WithEmptyMessages_ReturnsEmptyStream()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "gpt-4",
            Messages = new List<OpenAIMessage>(),
        };
        var messages = new List<ChatMessage>();

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._translatorMock.Setup(x => x.TranslateUpdate(It.IsAny<ChatResponseUpdate>())).Returns<ChatResponseUpdate>(u => u);
        this._chatClientMock.Setup(x => x.GetStreamingResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<ChatResponseUpdate>());

        var service = this.CreateService();

        var results = new List<AgentsAI.AgentResponseUpdate>();
        await foreach (var update in service.HandleStreamingAsync(openAIRequest, messages))
        {
            results.Add(update);
        }

        Assert.Empty(results);
    }

    [Fact]
    public async Task HandleStreamingAsync_TranslatesEachUpdate()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "gpt-4",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var updates = new List<ChatResponseUpdate>
        {
            new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = { new TextContent("Hello") } },
            new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = { new TextContent("World") } },
        };

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._chatClientMock.Setup(x => x.GetStreamingResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .Returns(updates.ToAsyncEnumerable());

        var translatedUpdate = new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = { new TextContent("Translated") } };
        this._translatorMock.Setup(x => x.TranslateUpdate(It.IsAny<ChatResponseUpdate>())).Returns(translatedUpdate);

        var service = this.CreateService();

        var results = new List<AgentsAI.AgentResponseUpdate>();
        await foreach (var update in service.HandleStreamingAsync(openAIRequest, messages))
        {
            results.Add(update);
        }

        Assert.Equal(2, results.Count);
        this._translatorMock.Verify(x => x.TranslateUpdate(It.IsAny<ChatResponseUpdate>()), Times.Exactly(2));
    }

    [Fact]
    public async Task HandleStreamingAsync_PassesCorrectModelIdToChatOptions()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "custom-model",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "translated-model", messages);

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._translatorMock.Setup(x => x.TranslateUpdate(It.IsAny<ChatResponseUpdate>())).Returns<ChatResponseUpdate>(u => u);
        this._chatClientMock.Setup(x => x.GetStreamingResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<ChatResponseUpdate>());

        var service = this.CreateService();

        await foreach (var _ in service.HandleStreamingAsync(openAIRequest, messages)) { }

        this._chatClientMock.Verify(x => x.GetStreamingResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.Is<ChatOptions>(o => o.ModelId == "translated-model"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleStreamingAsync_WithCancellationToken_PassesTokenToChatClient()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "gpt-4",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };
        var cancellationToken = new CancellationToken(true);

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._translatorMock.Setup(x => x.TranslateUpdate(It.IsAny<ChatResponseUpdate>())).Returns<ChatResponseUpdate>(u => u);
        this._chatClientMock.Setup(x => x.GetStreamingResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), cancellationToken))
            .Returns(AsyncEnumerable.Empty<ChatResponseUpdate>());

        var service = this.CreateService();

        await foreach (var _ in service.HandleStreamingAsync(openAIRequest, messages, cancellationToken: cancellationToken)) { }

        this._chatClientMock.Verify(x => x.GetStreamingResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task HandleStreamingAsync_WithNullHttpContext_DoesNotThrow()
    {
        this._httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var openAIRequest = new OpenAIRequest
        {
            Model = "gpt-4",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._translatorMock.Setup(x => x.TranslateUpdate(It.IsAny<ChatResponseUpdate>())).Returns<ChatResponseUpdate>(u => u);
        this._chatClientMock.Setup(x => x.GetStreamingResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<ChatResponseUpdate>());

        var service = this.CreateService();

        var results = new List<AgentsAI.AgentResponseUpdate>();
        await foreach (var update in service.HandleStreamingAsync(openAIRequest, messages))
        {
            results.Add(update);
        }

        Assert.Empty(results);
    }

    #endregion

    #region RoutingContext Tests

    [Fact]
    public async Task HandleAsync_WithRoutingContextProperties_SetsCorrectValues()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "requested-model",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello!"));
        chatResponse.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            ["model_id"] = "resolved-model",
            ["provider_name"] = "test-provider",
        };

        var translatedResponse = new CanonicalResponse("Hello!");

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._chatClientMock.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        this._translatorMock.Setup(x => x.TranslateResponse(It.IsAny<CanonicalResponse>())).Returns(translatedResponse);

        var service = this.CreateService();

        await service.HandleAsync(openAIRequest, messages);

        var routingContext = this._httpContext.Items["RoutingContext"] as RoutingContext;
        Assert.NotNull(routingContext);
        Assert.Equal("requested-model", routingContext.RequestedModel);
        Assert.Equal("resolved-model", routingContext.ResolvedCanonicalId);
        Assert.Equal("test-provider", routingContext.Provider);
    }

    [Fact]
    public async Task HandleAsync_WithoutAdditionalProperties_DoesNotSetRoutingContext()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "gpt-4",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello!"));

        var translatedResponse = new CanonicalResponse("Hello!");

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._chatClientMock.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        this._translatorMock.Setup(x => x.TranslateResponse(It.IsAny<CanonicalResponse>())).Returns(translatedResponse);

        var service = this.CreateService();

        await service.HandleAsync(openAIRequest, messages);

        Assert.False(this._httpContext.Items.ContainsKey("RoutingContext"));
    }

    [Fact]
    public async Task HandleAsync_WithPartialAdditionalProperties_DoesNotSetRoutingContext()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = "gpt-4",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "gpt-4", messages);
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello!"));
        chatResponse.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            ["model_id"] = "resolved-model",
        };

        var translatedResponse = new CanonicalResponse("Hello!");

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._chatClientMock.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        this._translatorMock.Setup(x => x.TranslateResponse(It.IsAny<CanonicalResponse>())).Returns(translatedResponse);

        var service = this.CreateService();

        await service.HandleAsync(openAIRequest, messages);

        Assert.False(this._httpContext.Items.ContainsKey("RoutingContext"));
    }

    [Fact]
    public async Task HandleAsync_WithNullModel_UsesDefaultInRoutingContext()
    {
        var openAIRequest = new OpenAIRequest
        {
            Model = null!,
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello" }
            },
        };
        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hello") };

        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, "default", messages);
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello!"));
        chatResponse.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            ["model_id"] = "resolved-model",
            ["provider_name"] = "test-provider",
        };

        var translatedResponse = new CanonicalResponse("Hello!");

        this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);
        this._chatClientMock.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
        this._translatorMock.Setup(x => x.TranslateResponse(It.IsAny<CanonicalResponse>())).Returns(translatedResponse);

        var service = this.CreateService();

        await service.HandleAsync(openAIRequest, messages);

        var routingContext = this._httpContext.Items["RoutingContext"] as RoutingContext;
        Assert.NotNull(routingContext);
        Assert.Equal("default", routingContext.RequestedModel);
    }

    #endregion
}
