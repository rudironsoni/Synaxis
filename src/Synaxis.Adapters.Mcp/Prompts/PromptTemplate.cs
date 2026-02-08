// <copyright file="PromptTemplate.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp.Prompts
{
    /// <summary>
    /// Represents a prompt template in the MCP system.
    /// </summary>
    /// <param name="Name">The unique template name.</param>
    /// <param name="Description">A description of what the prompt does.</param>
    /// <param name="Template">The prompt template with {{variable}} placeholders.</param>
    /// <param name="Variables">The list of required variable names.</param>
    public sealed record PromptTemplate(
        string Name,
        string Description,
        string Template,
        string[] Variables);
}
