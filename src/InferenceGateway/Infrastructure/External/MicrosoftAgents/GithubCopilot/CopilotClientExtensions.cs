// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using Microsoft.Agents.AI.Abstractions;
using Synaxis.InferenceGateway.Infrastructure.External.MicrosoftAgents.GithubCopilot;
using Microsoft.Extensions.AI;
namespace GitHub.Copilot.SDK;

/// <summary>
/// Provides extension methods for <see cref="CopilotClient"/>
/// to simplify the creation of GitHub Copilot agents.
/// </summary>
public static class CopilotClientExtensions
{
    /// <summary>
    /// Retrieves an instance of <see cref="AIAgent"/> for a GitHub Copilot client.
    /// </summary>
    public static AIAgent AsAIAgent(
        this CopilotClient client,
        SessionConfig? sessionConfig = null,
        bool ownsClient = false,
        string? id = null,
        string? name = null,
        string? description = null)
    {
        if (client is null) throw new ArgumentNullException(nameof(client));

        return new GithubCopilotAgent(client, sessionConfig, ownsClient, id, name, description);
    }

    /// <summary>
    /// Retrieves an instance of <see cref="AIAgent"/> for a GitHub Copilot client.
    /// </summary>
    public static AIAgent AsAIAgent(
        this CopilotClient client,
        bool ownsClient = false,
        string? id = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        string? instructions = null)
    {
        if (client is null) throw new ArgumentNullException(nameof(client));

        return new GithubCopilotAgent(client, ownsClient, id, name, description, tools, instructions);
    }
}
