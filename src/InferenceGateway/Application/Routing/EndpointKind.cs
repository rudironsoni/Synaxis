// <copyright file="EndpointKind.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing;

/// <summary>
/// Defines the type of API endpoint for routing inference requests.
/// </summary>
public enum EndpointKind
{
    /// <summary>
    /// Modern chat completions endpoint for conversational AI interactions.
    /// </summary>
    ChatCompletions,

    /// <summary>
    /// Streaming responses endpoint for real-time inference output.
    /// </summary>
    Responses,

    /// <summary>
    /// Legacy completions endpoint for backward compatibility with older API contracts.
    /// </summary>
    LegacyCompletions,
}
