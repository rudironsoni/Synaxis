// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.Agents.AI;

namespace Synaxis.InferenceGateway.Infrastructure.External.MicrosoftAgents.GithubCopilot;

/// <summary>
/// Represents a session for a GitHub Copilot agent conversation.
/// </summary>
public sealed class GithubCopilotAgentSession : Microsoft.Agents.AI.AgentSession
{
    public string? SessionId { get; internal set; }

    internal GithubCopilotAgentSession() { }

    internal GithubCopilotAgentSession(JsonElement serializedThread, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        if (serializedThread.TryGetProperty("sessionId", out JsonElement sessionIdElement))
        {
            this.SessionId = sessionIdElement.GetString();
        }
    }

    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        State state = new() { SessionId = this.SessionId };
        return JsonSerializer.SerializeToElement(
            state,
            GithubCopilotJsonUtilities.DefaultOptions.GetTypeInfo(typeof(State)));
    }

    internal sealed class State
    {
        public string? SessionId { get; set; }
    }
}
