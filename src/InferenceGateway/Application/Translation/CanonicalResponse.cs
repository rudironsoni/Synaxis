// <copyright file="CanonicalResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using Microsoft.Extensions.AI;

    /// <summary>
    /// Represents a canonical response with content and optional tool calls.
    /// </summary>
    /// <param name="Content">The response content text.</param>
    /// <param name="ToolCalls">The list of function call contents representing tool calls.</param>
    public sealed record CanonicalResponse(string? Content, IList<FunctionCallContent>? ToolCalls = null);
}
