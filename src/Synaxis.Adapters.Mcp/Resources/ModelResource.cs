// <copyright file="ModelResource.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp.Resources
{
    /// <summary>
    /// Represents a model resource in the MCP system.
    /// </summary>
    /// <param name="Id">The unique model identifier.</param>
    /// <param name="Name">The display name of the model.</param>
    /// <param name="Provider">The provider that hosts this model.</param>
    /// <param name="Capabilities">The capabilities supported by this model.</param>
    /// <param name="ContextWindow">The maximum context window size in tokens.</param>
    /// <param name="MaxOutputTokens">The maximum output tokens the model can generate.</param>
    public sealed record ModelResource(
        string Id,
        string Name,
        string Provider,
        string[] Capabilities,
        int ContextWindow,
        int? MaxOutputTokens);
}
