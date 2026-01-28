using Mediator;
using Microsoft.Extensions.AI;
using Synaxis.InferenceGateway.Application.Translation;
using System.Collections.Generic;
using Microsoft.Agents;

namespace Synaxis.InferenceGateway.WebApi.Features.Chat.Commands;

// Command for non-streaming chat completion
public record ChatCommand(OpenAIRequest Request, IEnumerable<ChatMessage> Messages) 
    : IRequest<AgentResponse>;

// Command for streaming chat completion
public record ChatStreamCommand(OpenAIRequest Request, IEnumerable<ChatMessage> Messages) 
    : IStreamRequest<AgentResponseUpdate>;
