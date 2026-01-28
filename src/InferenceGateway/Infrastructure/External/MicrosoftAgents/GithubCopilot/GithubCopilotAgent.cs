// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.Abstractions;

namespace Synaxis.InferenceGateway.Infrastructure.External.MicrosoftAgents.GithubCopilot;

/// <summary>
/// Represents an <see cref="AIAgent"/> that uses the GitHub Copilot SDK to provide agentic capabilities.
/// </summary>
public sealed class GithubCopilotAgent : AIAgent, IAsyncDisposable
{
    private const string DefaultName = "GitHub Copilot Agent";
    private const string DefaultDescription = "An AI agent powered by GitHub Copilot";

    private readonly CopilotClient _copilotClient;
    private readonly string? _id;
    private readonly string _name;
    private readonly string _description;
    private readonly SessionConfig? _sessionConfig;
    private readonly bool _ownsClient;

    public GithubCopilotAgent(
        CopilotClient copilotClient,
        SessionConfig? sessionConfig = null,
        bool ownsClient = false,
        string? id = null,
        string? name = null,
        string? description = null)
    {
        if (copilotClient is null) throw new ArgumentNullException(nameof(copilotClient));
        this._copilotClient = copilotClient;
        this._sessionConfig = sessionConfig;
        this._ownsClient = ownsClient;
        this._id = id;
        this._name = name ?? DefaultName;
        this._description = description ?? DefaultDescription;
    }

    public GithubCopilotAgent(
        CopilotClient copilotClient,
        bool ownsClient = false,
        string? id = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        string? instructions = null)
        : this(
            copilotClient,
            GetSessionConfig(tools, instructions),
            ownsClient,
            id,
            name,
            description)
    {
    }

    public sealed override ValueTask<AgentSession> GetNewSessionAsync(CancellationToken cancellationToken = default)
        => new(new GithubCopilotAgentSession());

    public ValueTask<AgentSession> GetNewSessionAsync(string sessionId)
        => new(new GithubCopilotAgentSession() { SessionId = sessionId });

    public override ValueTask<AgentSession> DeserializeSessionAsync(
        JsonElement serializedSession,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
        => new(new GithubCopilotAgentSession(serializedSession, jsonSerializerOptions));

    protected override Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
        => this.RunCoreStreamingAsync(messages, session, options, cancellationToken).ToAgentResponseAsync(cancellationToken);

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (messages is null) throw new ArgumentNullException(nameof(messages));

        session ??= await this.GetNewSessionAsync(cancellationToken).ConfigureAwait(false);
        if (session is not GithubCopilotAgentSession typedSession)
        {
            throw new InvalidOperationException(
                $"The provided session type {session.GetType()} is not compatible with the agent. Only GitHub Copilot agent created sessions are supported.");
        }

        await this.EnsureClientStartedAsync(cancellationToken).ConfigureAwait(false);

        SessionConfig sessionConfig = this._sessionConfig != null
            ? new SessionConfig
            {
                Model = this._sessionConfig.Model,
                Tools = this._sessionConfig.Tools,
                SystemMessage = this._sessionConfig.SystemMessage,
                AvailableTools = this._sessionConfig.AvailableTools,
                ExcludedTools = this._sessionConfig.ExcludedTools,
                Provider = this._sessionConfig.Provider,
                OnPermissionRequest = this._sessionConfig.OnPermissionRequest,
                McpServers = this._sessionConfig.McpServers,
                CustomAgents = this._sessionConfig.CustomAgents,
                SkillDirectories = this._sessionConfig.SkillDirectories,
                DisabledSkills = this._sessionConfig.DisabledSkills,
                Streaming = true
            }
            : new SessionConfig { Streaming = true };

        CopilotSession copilotSession;
        if (typedSession.SessionId is not null)
        {
            copilotSession = await this._copilotClient.ResumeSessionAsync(
                typedSession.SessionId,
                this.CreateResumeConfig(),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            copilotSession = await this._copilotClient.CreateSessionAsync(sessionConfig, cancellationToken).ConfigureAwait(false);
            typedSession.SessionId = copilotSession.SessionId;
        }

        try
        {
            Channel<AgentResponseUpdate> channel = Channel.CreateUnbounded<AgentResponseUpdate>();

            using IDisposable subscription = copilotSession.On(evt =>
            {
                switch (evt)
                {
                    case AssistantMessageDeltaEvent deltaEvent:
                        channel.Writer.TryWrite(this.ConvertToAgentResponseUpdate(deltaEvent));
                        break;
                    case AssistantMessageEvent assistantMessage:
                        channel.Writer.TryWrite(this.ConvertToAgentResponseUpdate(assistantMessage));
                        break;
                    case AssistantUsageEvent usageEvent:
                        channel.Writer.TryWrite(this.ConvertToAgentResponseUpdate(usageEvent));
                        break;
                    case SessionIdleEvent idleEvent:
                        channel.Writer.TryWrite(this.ConvertToAgentResponseUpdate(idleEvent));
                        channel.Writer.TryComplete();
                        break;
                    case SessionErrorEvent errorEvent:
                        channel.Writer.TryWrite(this.ConvertToAgentResponseUpdate(errorEvent));
                        channel.Writer.TryComplete(new InvalidOperationException(
                            $"Session error: {errorEvent.Data?.Message ?? "Unknown error"}"));
                        break;
                    default:
                        channel.Writer.TryWrite(this.ConvertToAgentResponseUpdate(evt));
                        break;
                }
            });

            try
            {
                string prompt = string.Join("\n", messages.Select(m => m.Text));
                MessageOptions messageOptions = new() { Prompt = prompt };

                await copilotSession.SendAsync(messageOptions, cancellationToken).ConfigureAwait(false);
                await foreach (AgentResponseUpdate update in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    yield return update;
                }
            }
            finally
            {
            }
        }
        finally
        {
            await copilotSession.DisposeAsync().ConfigureAwait(false);
        }
    }

    protected override string? IdCore => this._id;
    public override string Name => this._name;
    public override string Description => this._description;

    public async ValueTask DisposeAsync()
    {
        if (this._ownsClient)
        {
            await this._copilotClient.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task EnsureClientStartedAsync(CancellationToken cancellationToken)
    {
        if (this._copilotClient.State != ConnectionState.Connected)
        {
            await this._copilotClient.StartAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private ResumeSessionConfig CreateResumeConfig()
    {
        return new ResumeSessionConfig
        {
            Tools = this._sessionConfig?.Tools,
            Provider = this._sessionConfig?.Provider,
            OnPermissionRequest = this._sessionConfig?.OnPermissionRequest,
            McpServers = this._sessionConfig?.McpServers,
            CustomAgents = this._sessionConfig?.CustomAgents,
            SkillDirectories = this._sessionConfig?.SkillDirectories,
            DisabledSkills = this._sessionConfig?.DisabledSkills,
            Streaming = true
        };
    }

    private AgentResponseUpdate ConvertToAgentResponseUpdate(AssistantMessageDeltaEvent deltaEvent)
    {
        TextContent textContent = new(deltaEvent.Data?.DeltaContent ?? string.Empty)
        {
            RawRepresentation = deltaEvent
        };

        var update = new AgentResponseUpdate { Role = ChatRole.Assistant };
        update.Contents.Add(textContent);
        update.AgentId = this.Id;
        update.MessageId = deltaEvent.Data?.MessageId;
        update.CreatedAt = deltaEvent.Timestamp;
        return update;
    }

    private AgentResponseUpdate ConvertToAgentResponseUpdate(AssistantMessageEvent assistantMessage)
    {
        TextContent textContent = new(assistantMessage.Data?.Content ?? string.Empty)
        {
            RawRepresentation = assistantMessage
        };

        var update = new AgentResponseUpdate { Role = ChatRole.Assistant };
        update.Contents.Add(textContent);
        update.AgentId = this.Id;
        update.ResponseId = assistantMessage.Data?.MessageId;
        update.MessageId = assistantMessage.Data?.MessageId;
        update.CreatedAt = assistantMessage.Timestamp;
        return update;
    }

    private AgentResponseUpdate ConvertToAgentResponseUpdate(AssistantUsageEvent usageEvent)
    {
        UsageDetails usageDetails = new()
        {
            InputTokenCount = (int?)(usageEvent.Data?.InputTokens),
            OutputTokenCount = (int?)(usageEvent.Data?.OutputTokens),
            TotalTokenCount = (int?)((usageEvent.Data?.InputTokens ?? 0) + (usageEvent.Data?.OutputTokens ?? 0)),
            CachedInputTokenCount = (int?)(usageEvent.Data?.CacheReadTokens),
            AdditionalCounts = GetAdditionalCounts(usageEvent),
        };

        UsageContent usageContent = new(usageDetails)
        {
            RawRepresentation = usageEvent
        };

        var update = new AgentResponseUpdate { Role = ChatRole.Assistant };
        update.Contents.Add(usageContent);
        update.AgentId = this.Id;
        update.CreatedAt = usageEvent.Timestamp;
        return update;
    }

    private static AdditionalPropertiesDictionary<long>? GetAdditionalCounts(AssistantUsageEvent usageEvent)
    {
        if (usageEvent.Data is null) return null;
        AdditionalPropertiesDictionary<long>? additionalCounts = null;
        if (usageEvent.Data.CacheWriteTokens is double cacheWriteTokens)
        {
            additionalCounts ??= [];
            additionalCounts[nameof(AssistantUsageData.CacheWriteTokens)] = (long)cacheWriteTokens;
        }
        if (usageEvent.Data.Cost is double cost)
        {
            additionalCounts ??= [];
            additionalCounts[nameof(AssistantUsageData.Cost)] = (long)cost;
        }
        if (usageEvent.Data.Duration is double duration)
        {
            additionalCounts ??= [];
            additionalCounts[nameof(AssistantUsageData.Duration)] = (long)duration;
        }
        return additionalCounts;
    }

    private AgentResponseUpdate ConvertToAgentResponseUpdate(SessionEvent sessionEvent)
    {
        AIContent content = new() { RawRepresentation = sessionEvent };
        var update = new AgentResponseUpdate { Role = ChatRole.Assistant };
        update.Contents.Add(content);
        update.AgentId = this.Id;
        update.CreatedAt = sessionEvent.Timestamp;
        return update;
    }

    private static SessionConfig? GetSessionConfig(IList<AITool>? tools, string? instructions)
    {
        List<AIFunction>? mappedTools = tools is { Count: > 0 } ? tools.OfType<AIFunction>().ToList() : null;
        SystemMessageConfig? systemMessage = instructions is not null ? new SystemMessageConfig { Mode = SystemMessageMode.Append, Content = instructions } : null;

        if (mappedTools is null && systemMessage is null) return null;
        return new SessionConfig { Tools = mappedTools, SystemMessage = systemMessage };
    }

    // Attachment handling is currently unsupported. Only text prompts are supported.
}
