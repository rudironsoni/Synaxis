using Mediator;
using Microsoft.Agents;
using Synaxis.InferenceGateway.WebApi.Agents;
using Synaxis.InferenceGateway.WebApi.Features.Chat.Commands;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.WebApi.Features.Chat.Handlers;

public class ChatCompletionHandler : 
    IRequestHandler<ChatCommand, AgentResponse>,
    IStreamRequestHandler<ChatStreamCommand, AgentResponseUpdate>
{
    private readonly RoutingAgent _agent;

    public ChatCompletionHandler(RoutingAgent agent)
    {
        _agent = agent;
    }

    public async ValueTask<AgentResponse> Handle(ChatCommand request, CancellationToken cancellationToken)
    {
        return await _agent.RunAsync(request.Messages, cancellationToken: cancellationToken);
    }

    public IAsyncEnumerable<AgentResponseUpdate> Handle(ChatStreamCommand request, CancellationToken cancellationToken)
    {
        return _agent.RunStreamingAsync(request.Messages, cancellationToken: cancellationToken);
    }
}
