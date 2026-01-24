using Synaplexer.Contracts.Grpc;
using Synaplexer.Application.Commands;
using Synaplexer.Application.Dtos;
using Grpc.Core;
using Mediator;

namespace Synaplexer.API.GrpcServices;

public class LlmGrpcService : LlmService.LlmServiceBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LlmGrpcService> _logger;

    public LlmGrpcService(IMediator mediator, ILogger<LlmGrpcService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task ChatCompletion(
        ChatRequest request, 
        IServerStreamWriter<ChatResponse> responseStream, 
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC ChatCompletion request for model {Model}", request.Model);

        var messages = request.Messages.Select(m => new Synaplexer.Application.Dtos.ChatMessage(m.Role, m.Content)).ToArray();
        
        var command = new ChatCompletionCommand(
            request.Model,
            messages,
            (float)request.Temperature,
            2048, // Default max tokens
            true  // Stream is true for gRPC stream response
        );

        // For now, since the handler returns a single result, we send it as one chunk
        // In a real implementation, the handler would return IAsyncEnumerable
        var result = await _mediator.Send(command, context.CancellationToken);

        await responseStream.WriteAsync(new ChatResponse
        {
            Content = result.Content,
            IsFinished = true
        });
    }
}
