// <copyright file="CanonicalChunk.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using Microsoft.Extensions.AI;

    /// <summary>
    /// Represents a canonical streaming chunk with content delta and optional tool call deltas.
    /// </summary>
    /// <param name="contentDelta">The incremental content text.</param>
    /// <param name="toolCallDeltas">The list of function call content deltas.</param>
    public sealed record CanonicalChunk(string? contentDelta, IList<FunctionCallContent>? toolCallDeltas = null);
}
