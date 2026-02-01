using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.Application.Translation;
using Synaxis.InferenceGateway.WebApi.Agents;
using Synaxis.InferenceGateway.WebApi.Middleware;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests.Agents;

public class RoutingAgentTests
{
    private readonly Mock<IChatClient> _chatClientMock;
    private readonly Mock<ITranslationPipeline> _translatorMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<RoutingAgent>> _loggerMock;
    private readonly RoutingAgent _agent;

    public RoutingAgentTests()
    {
        _chatClientMock = new Mock<IChatClient>();
        _translatorMock = new Mock<ITranslationPipeline>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<RoutingAgent>>();

        _agent = new RoutingAgent(
            _chatClientMock.Object,
            _translatorMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RunCoreAsync_DelegatesToChatClient_WithCorrectModel()
    {
        // Arrange
        var messages = new List<ChatMessage> { new(ChatRole.User, "hello") };
        var modelId = "requested-model";
        var openAIRequest = new OpenAIRequest 
        { 
            Model = modelId,
            Messages = new List<OpenAIMessage> { new OpenAIMessage { Role = "user", Content = "hello" } }
        };
        var jsonBody = JsonSerializer.Serialize(openAIRequest);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));

        var context = new DefaultHttpContext();
        context.Request.Body = bodyStream;
        context.Request.ContentLength = bodyStream.Length;
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        // Translator setup (pass-through)
        var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, modelId, messages);
        _translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);

        var translatedResponse = new CanonicalResponse("Response", new List<FunctionCallContent>());
        _translatorMock.Setup(x => x.TranslateResponse(It.IsAny<CanonicalResponse>())).Returns(translatedResponse);

        // ChatClient setup
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "raw-response"));
        expectedResponse.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            { "model_id", "actual-model-v1" },
            { "provider_name", "provider-a" }
        };

        _chatClientMock.Setup(x => x.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.Is<ChatOptions>(o => o.ModelId == modelId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _agent.RunAsync(messages);

        // Assert
        Assert.NotNull(result);

        // Verify RoutingContext in HttpContext
        Assert.True(context.Items.ContainsKey("RoutingContext"));
        var routingContext = context.Items["RoutingContext"] as RoutingContext;
        Assert.NotNull(routingContext);
        Assert.Equal(modelId, routingContext.RequestedModel);
        Assert.Equal("actual-model-v1", routingContext.ResolvedCanonicalId);
        Assert.Equal("provider-a", routingContext.Provider);
    }
}
