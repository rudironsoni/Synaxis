// <copyright file="RoutingAgentTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests.Agents
{
    public class RoutingAgentTests
    {
        private readonly Mock<IChatClient> _chatClientMock;
        private readonly Mock<ITranslationPipeline> _translatorMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly RoutingAgent _agent;

        public RoutingAgentTests()
        {
            this._chatClientMock = new Mock<IChatClient>();
            this._translatorMock = new Mock<ITranslationPipeline>();
            this._httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            this._agent = new RoutingAgent(
                this._chatClientMock.Object,
                this._translatorMock.Object,
                this._httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task RunCoreAsync_DelegatesToChatClient_WithCorrectModel()
        {
            // Arrange
            var messages = new List<ChatMessage> { new (ChatRole.User, "hello") };
            var modelId = "requested-model";
            var openAIRequest = new OpenAIRequest
            {
                Model = modelId,
                Messages = new List<OpenAIMessage> { new OpenAIMessage { Role = "user", Content = "hello" } },
            };
            var jsonBody = JsonSerializer.Serialize(openAIRequest);
            var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));

            var context = new DefaultHttpContext();
            context.Request.Body = bodyStream;
            context.Request.ContentLength = bodyStream.Length;
            this._httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

            // Translator setup (pass-through)
            var translatedRequest = new CanonicalRequest(EndpointKind.ChatCompletions, modelId, messages);
            this._translatorMock.Setup(x => x.TranslateRequest(It.IsAny<CanonicalRequest>())).Returns(translatedRequest);

            var translatedResponse = new CanonicalResponse("Response", new List<FunctionCallContent>());
            this._translatorMock.Setup(x => x.TranslateResponse(It.IsAny<CanonicalResponse>())).Returns(translatedResponse);

            // ChatClient setup
            var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "raw-response"));
            expectedResponse.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            { "model_id", "actual-model-v1" },
            { "provider_name", "provider-a" },
        };

            this._chatClientMock.Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.Is<ChatOptions>(o => o.ModelId == modelId),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await this._agent.RunAsync(messages);

            // Assert
            Assert.NotNull(result);

            // Verify RoutingContext in HttpContext
            Assert.True(context.Items.ContainsKey("RoutingContext"));
            var routingContext = context.Items["RoutingContext"] as RoutingContext;
            Assert.NotNull(routingContext);
            Assert.Equal(modelId, routingContext.requestedModel);
            Assert.Equal("actual-model-v1", routingContext.resolvedCanonicalId);
            Assert.Equal("provider-a", routingContext.provider);
        }
    }
}
