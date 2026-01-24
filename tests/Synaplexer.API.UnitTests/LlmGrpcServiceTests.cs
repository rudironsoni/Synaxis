using Synaplexer.Contracts.Grpc;
using Synaplexer.API.GrpcServices;
using Synaplexer.Application.Commands;
using Synaplexer.Application.Dtos;
using Grpc.Core;
using Mediator;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using FluentAssertions;

namespace Synaplexer.Api.Tests;

public class LlmGrpcServiceTests
{
    private readonly IMediator _mediator;
    private readonly ILogger<LlmGrpcService> _logger;
    private readonly LlmGrpcService _service;

    public LlmGrpcServiceTests()
    {
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<LlmGrpcService>>();
        _service = new LlmGrpcService(_mediator, _logger);
    }

    [Fact]
    public async Task ChatCompletion_CallsMediatorAndWritesToStream()
    {
        // Arrange
        var request = new ChatRequest
        {
            Model = "gpt-4",
            Temperature = 0.7f
        };
        request.Messages.Add(new Synaplexer.Contracts.Grpc.ChatMessage { Role = "user", Content = "Hello" });

        var expectedResult = new ChatCompletionResult(
            Content: "Hi there!",
            Model: "gpt-4",
            UsageTokens: 10,
            FinishReason: "stop"
        );

        _mediator.Send(Arg.Any<ChatCompletionCommand>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var streamWriter = Substitute.For<IServerStreamWriter<ChatResponse>>();
        var context = Substitute.For<ServerCallContext>();

        // Act
        await _service.ChatCompletion(request, streamWriter, context);

        // Assert
        await _mediator.Received(1).Send(Arg.Is<ChatCompletionCommand>(c => 
            c.Model == "gpt-4" && 
            c.Messages[0].Content == "Hello"), Arg.Any<CancellationToken>());
        
        await streamWriter.Received(1).WriteAsync(Arg.Is<ChatResponse>(r => 
            r.Content == "Hi there!" && r.IsFinished));
    }
}
