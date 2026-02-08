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
    /// <param name="Endpoint">The endpoint kind for routing.</param>
    /// <param name="Model">The model identifier.</param>
    /// <param name="Messages">The list of chat messages.</param>
    /// <param name="Tools">The list of available AI tools.</param>
    /// <param name="ToolChoice">The tool choice configuration.</param>
    /// <param name="ResponseFormat">The response format configuration.</param>
    /// <param name="AdditionalOptions">Additional chat options.</param>
    public sealed record CanonicalRequest(
        EndpointKind Endpoint,
        string Model,
        IReadOnlyList<ChatMessage> Messages,
        IList<AITool>? Tools = null,
        object? ToolChoice = null,
        object? ResponseFormat = null,
        ChatOptions? AdditionalOptions = null);
}
