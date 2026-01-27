using Microsoft.Extensions.AI;
using Synaxis.InferenceGateway.Application.Routing;

namespace Synaxis.InferenceGateway.Application.Translation;

public sealed record CanonicalRequest(
    EndpointKind Endpoint,
    string Model,
    IReadOnlyList<ChatMessage> Messages,
    IList<AITool>? Tools = null,
    object? ToolChoice = null,
    object? ResponseFormat = null,
    ChatOptions? AdditionalOptions = null);