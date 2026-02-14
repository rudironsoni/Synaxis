// <copyright file="ChatGrpcServiceTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.IntegrationTests.Transport.Grpc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Grpc.Core;
    using global::Grpc.Net.Client;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.InferenceGateway.Application;
    using Synaxis.InferenceGateway.Application.Configuration;
    using Synaxis.Transport.Grpc.DependencyInjection;
    using Synaxis.Transport.Grpc.V1;
    using Xunit.Abstractions;

    /// <summary>
    /// Integration tests for the ChatGrpcService.
    /// </summary>
    public class ChatGrpcServiceTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatGrpcServiceTests"/> class.
        /// </summary>
        /// <param name="fixture">The test fixture.</param>
        /// <param name="output">The test output helper.</param>
        public ChatGrpcServiceTests(SynaxisWebApplicationFactory fixture, ITestOutputHelper output)
        {
            this._factory = fixture;
            this._factory.OutputHelper = output;
            this._output = output;
        }

        /// <summary>
        /// Tests that a unary call to CreateCompletion returns a valid protobuf response.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Mediator source generator infrastructure issue")]
        public async Task CreateCompletion_UnaryCall_ReturnsProtobufResponse()
        {
            // Arrange
            using var client = this.CreateGrpcClient();

            var request = new ChatRequest
            {
                Model = "test-alias",
            };
            request.Messages.Add(new Message
            {
                Role = "user",
                Content = "Hello, gRPC!",
            });

            // Act
            var metadata = CreateAuthMetadata();
            var response = await client.Client.CreateCompletionAsync(request, metadata);

            // Assert
            Assert.NotNull(response);
            Assert.NotEmpty(response.Id);
            Assert.Equal("chat.completion", response.Object);
            Assert.NotEqual(0, response.Created);
            Assert.Equal("test-alias", response.Model);
            Assert.Single(response.Choices);
            Assert.Equal(0, response.Choices[0].Index);
            Assert.NotNull(response.Choices[0].Message);
            Assert.Equal("assistant", response.Choices[0].Message.Role);
            Assert.Contains("mock", response.Choices[0].Message.Content, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(response.Usage);
            Assert.True(response.Usage.PromptTokens > 0);
            Assert.True(response.Usage.CompletionTokens > 0);
            Assert.True(response.Usage.TotalTokens > 0);
        }

        /// <summary>
        /// Tests streaming call returns multiple messages.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Mediator source generator infrastructure issue")]
        public async Task StreamCompletion_StreamingCall_ReturnsMultipleMessages()
        {
            // Arrange
            using var client = this.CreateGrpcClient();

            var request = new ChatRequest
            {
                Model = "test-alias",
            };
            request.Messages.Add(new Message
            {
                Role = "user",
                Content = "Hello, streaming gRPC!",
            });

            // Act
            var metadata = CreateAuthMetadata();
            var chunks = new List<ChatStreamChunk>();
            using var streamingCall = client.Client.StreamCompletion(request, metadata);
            await foreach (var chunk in streamingCall.ResponseStream.ReadAllAsync())
            {
                chunks.Add(chunk);
                this._output.WriteLine($"Received chunk: {chunk.Id}, Choices: {chunk.Choices.Count}");
            }

            // Assert
            Assert.NotEmpty(chunks);
            Assert.True(chunks.Count >= 3, "Should receive at least 3 chunks (role, content, finish)");

            // Verify first chunk has role
            var firstChunk = chunks[0];
            Assert.NotNull(firstChunk);
            Assert.NotEmpty(firstChunk.Id);
            Assert.Equal("chat.completion.chunk", firstChunk.Object);
            Assert.NotEqual(0, firstChunk.Created);
            Assert.Equal("test-alias", firstChunk.Model);

            // Verify last chunk has finish reason
            var lastChunk = chunks[chunks.Count - 1];
            Assert.NotNull(lastChunk);
            Assert.Single(lastChunk.Choices);
            Assert.Equal("stop", lastChunk.Choices[0].FinishReason);
        }

        /// <summary>
        /// Tests that creating a completion with an invalid model returns NotFound.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Mediator source generator infrastructure issue")]
        public async Task CreateCompletion_InvalidModel_ReturnsNotFoundStatusCode()
        {
            // Arrange
            using var client = this.CreateGrpcClient();

            var request = new ChatRequest
            {
                Model = "non-existent-model",
            };
            request.Messages.Add(new Message
            {
                Role = "user",
                Content = "Hello!",
            });

            // Add authentication metadata
            var metadata = CreateAuthMetadata();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
#pragma warning disable IDISP005
                client.Client.CreateCompletionAsync(request, metadata));
#pragma warning restore IDISP005

            Assert.Equal(StatusCode.NotFound, exception.StatusCode);
            this._output.WriteLine($"Expected error: {exception.Status}");
        }

        /// <summary>
        /// Tests that authentication via gRPC metadata works correctly.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Mediator source generator infrastructure issue")]
        public async Task CreateCompletion_WithValidToken_ReturnsSuccess()
        {
            // Arrange
            using var client = this.CreateGrpcClient();

            var request = new ChatRequest
            {
                Model = "test-alias",
            };
            request.Messages.Add(new Message
            {
                Role = "user",
                Content = "Hello, authenticated gRPC!",
            });

            // Add authentication metadata
            var metadata = CreateAuthMetadata();

            // Act
            var response = await client.Client.CreateCompletionAsync(request, metadata);

            // Assert
            Assert.NotNull(response);
            Assert.NotEmpty(response.Id);
            Assert.Equal("chat.completion", response.Object);
        }

        /// <summary>
        /// Tests that authentication fails with invalid token.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Mediator source generator infrastructure issue")]
        public async Task CreateCompletion_WithInvalidToken_ReturnsUnauthenticated()
        {
            // Arrange
            using var client = this.CreateGrpcClient();

            var request = new ChatRequest
            {
                Model = "test-alias",
            };
            request.Messages.Add(new Message
            {
                Role = "user",
                Content = "Hello!",
            });

            // Add invalid authentication metadata
            var metadata = CreateAuthMetadata("invalid-token");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
#pragma warning disable IDISP005
                client.Client.CreateCompletionAsync(request, metadata));
#pragma warning restore IDISP005

            Assert.Equal(StatusCode.Unauthenticated, exception.StatusCode);
            this._output.WriteLine($"Expected authentication error: {exception.Status}");
        }

        /// <summary>
        /// Tests that CreateCompletion with temperature parameter works correctly.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Mediator source generator infrastructure issue")]
        public async Task CreateCompletion_WithTemperature_ReturnsResponse()
        {
            // Arrange
            using var client = this.CreateGrpcClient();

            var request = new ChatRequest
            {
                Model = "test-alias",
                Temperature = 0.7,
            };
            request.Messages.Add(new Message
            {
                Role = "user",
                Content = "Hello!",
            });

            // Act
            var metadata = CreateAuthMetadata();
            var response = await client.Client.CreateCompletionAsync(request, metadata);

            // Assert
            Assert.NotNull(response);
            Assert.NotEmpty(response.Id);
            Assert.Equal("chat.completion", response.Object);
        }

        /// <summary>
        /// Tests that CreateCompletion with max_tokens parameter works correctly.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Mediator source generator infrastructure issue")]
        public async Task CreateCompletion_WithMaxTokens_ReturnsResponse()
        {
            // Arrange
            using var client = this.CreateGrpcClient();

            var request = new ChatRequest
            {
                Model = "test-alias",
                MaxTokens = 100,
            };
            request.Messages.Add(new Message
            {
                Role = "user",
                Content = "Hello!",
            });

            // Act
            var metadata = CreateAuthMetadata();
            var response = await client.Client.CreateCompletionAsync(request, metadata);

            // Assert
            Assert.NotNull(response);
            Assert.NotEmpty(response.Id);
            Assert.Equal("chat.completion", response.Object);
        }

        /// <summary>
        /// Helper method to create authenticated metadata with a valid token.
        /// </summary>
        /// <returns>Metadata with authorization header.</returns>
        private static Metadata CreateAuthMetadata(string token = "valid-test-token")
        {
            return new Metadata
            {
                { "authorization", $"Bearer {token}" },
            };
        }

        /// <summary>
        /// Creates a gRPC client configured with the test server.
        /// </summary>
        /// <returns>A configured DisposableChatServiceClient instance.</returns>
        private DisposableChatServiceClient CreateGrpcClient()
        {
            var testServer = this._factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Add gRPC services
                    services.AddSynaxisTransportGrpc();
                });
            }).Server;

            // Create a gRPC channel using the test server's handler
            var httpClient = testServer.CreateHandler();
            var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
            {
                HttpClient = new HttpClient(httpClient),
                DisposeHttpClient = true,
            });

            return new DisposableChatServiceClient(new ChatServiceClient(channel), channel);
        }
    }

    /// <summary>
    /// Disposable wrapper for the ChatServiceClient.
    /// </summary>
    internal class DisposableChatServiceClient : IDisposable
    {
        private readonly ChatServiceClient _client;
        private readonly GrpcChannel _channel;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableChatServiceClient"/> class.
        /// </summary>
        /// <param name="client">The underlying gRPC client.</param>
        /// <param name="channel">The gRPC channel.</param>
        public DisposableChatServiceClient(ChatServiceClient client, GrpcChannel channel)
        {
            this._client = client ?? throw new ArgumentNullException(nameof(client));
            this._channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        /// <summary>
        /// Gets the underlying gRPC client.
        /// </summary>
        public ChatServiceClient Client => this._client;

        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers", "IDISP007:Don't dispose injected", Justification = "Channel is created locally, not injected")]
        public void Dispose()
        {
            this._channel?.Dispose();
        }
    }

    /// <summary>
    /// Client wrapper for the ChatService gRPC service.
    /// </summary>
    internal class ChatServiceClient
    {
        private static readonly Method<ChatRequest, Synaxis.Transport.Grpc.V1.ChatResponse> CreateCompletionMethod =
            new Method<ChatRequest, Synaxis.Transport.Grpc.V1.ChatResponse>(
                MethodType.Unary,
                "synaxis.v1.ChatService",
                "CreateCompletion",
                Marshallers.Create(SerializeMessage, DeserializeChatRequest),
                Marshallers.Create(SerializeMessage, DeserializeChatResponse));

        private static readonly Method<ChatRequest, ChatStreamChunk> StreamCompletionMethod =
            new Method<ChatRequest, ChatStreamChunk>(
                MethodType.ServerStreaming,
                "synaxis.v1.ChatService",
                "StreamCompletion",
                Marshallers.Create(SerializeMessage, DeserializeChatRequest),
                Marshallers.Create(SerializeMessage, DeserializeChatStreamChunk));

        private readonly GrpcChannel _channel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatServiceClient"/> class.
        /// </summary>
        /// <param name="channel">The gRPC channel.</param>
        public ChatServiceClient(GrpcChannel channel)
        {
            this._channel = channel ?? throw new ArgumentNullException(nameof(channel));
        }

        /// <summary>
        /// Creates a non-streaming chat completion.
        /// </summary>
        /// <param name="request">The chat request.</param>
        /// <param name="metadata">Optional metadata for the call.</param>
        /// <returns>A <see cref="Task{Synaxis.Transport.Grpc.V1.ChatResponse}"/> representing the asynchronous operation.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers", "IDISP005:Return type should indicate that the value should be disposed", Justification = "Task is awaited by caller")]
        public async Task<Synaxis.Transport.Grpc.V1.ChatResponse> CreateCompletionAsync(ChatRequest request, Metadata? metadata = null)
        {
            var callInvoker = this._channel.CreateCallInvoker();
            var callOptions = metadata != null ? new CallOptions(metadata) : default;

            return await callInvoker.AsyncUnaryCall(
                CreateCompletionMethod,
                null,
                callOptions,
                request).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a streaming chat completion.
        /// </summary>
        /// <param name="request">The chat request.</param>
        /// <param name="metadata">Optional metadata for the call.</param>
        /// <returns>A <see cref="AsyncServerStreamingCall{ChatStreamChunk}"/> for reading the stream.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers", "IDISP005:Return type should indicate that the value should be disposed", Justification = "Caller is responsible for disposing the AsyncServerStreamingCall")]
        public AsyncServerStreamingCall<ChatStreamChunk> StreamCompletion(ChatRequest request, Metadata? metadata = null)
        {
            var callInvoker = this._channel.CreateCallInvoker();
            var callOptions = metadata != null ? new CallOptions(metadata) : default;

            return callInvoker.AsyncServerStreamingCall(
                StreamCompletionMethod,
                null,
                callOptions,
                request);
        }

        private static void SerializeMessage<T>(T message, global::Grpc.Core.SerializationContext context)
            where T : global::Google.Protobuf.IMessage<T>
        {
            context.SetPayloadLength(message.CalculateSize());
            global::Google.Protobuf.MessageExtensions.WriteTo(message, context.GetBufferWriter());
            context.Complete();
        }

        private static ChatRequest DeserializeChatRequest(global::Grpc.Core.DeserializationContext context)
        {
            return ChatRequest.Parser.ParseFrom(context.PayloadAsReadOnlySequence());
        }

        private static Synaxis.Transport.Grpc.V1.ChatResponse DeserializeChatResponse(global::Grpc.Core.DeserializationContext context)
        {
            return Synaxis.Transport.Grpc.V1.ChatResponse.Parser.ParseFrom(context.PayloadAsReadOnlySequence());
        }

        private static ChatStreamChunk DeserializeChatStreamChunk(global::Grpc.Core.DeserializationContext context)
        {
            return ChatStreamChunk.Parser.ParseFrom(context.PayloadAsReadOnlySequence());
        }
    }
}
