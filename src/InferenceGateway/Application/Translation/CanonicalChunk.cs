using Microsoft.Extensions.AI;

namespace Synaxis.InferenceGateway.Application.Translation;

public sealed record CanonicalChunk(string? ContentDelta, IList<FunctionCallContent>? ToolCallDeltas = null);
