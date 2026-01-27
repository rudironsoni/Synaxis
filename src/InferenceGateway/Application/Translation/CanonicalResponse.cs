using Microsoft.Extensions.AI;

namespace Synaxis.InferenceGateway.Application.Translation;

public sealed record CanonicalResponse(string? Content, IList<FunctionCallContent>? ToolCalls = null);
