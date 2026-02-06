// <copyright file="CanonicalRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using Microsoft.Extensions.AI;
    using Synaxis.InferenceGateway.Application.Routing;

    /// <summary>
    /// Represents a canonical request containing all necessary information for inference.
    /// </summary>
    /// <param name="endpoint">The endpoint kind for routing.</param>
    /// <param name="model">The model identifier.</param>
    /// <param name="messages">The list of chat messages.</param>
    /// <param name="tools">The list of available AI tools.</param>
    /// <param name="toolChoice">The tool choice configuration.</param>
    /// <param name="responseFormat">The response format configuration.</param>
    /// <param name="additionalOptions">Additional chat options.</param>
    public sealed record CanonicalRequest(
        EndpointKind endpoint,
        string model,
        IReadOnlyList<ChatMessage> messages,
        IList<AITool>? tools = null,
        object? toolChoice = null,
        object? responseFormat = null,
        ChatOptions? additionalOptions = null);
}
