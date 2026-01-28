using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Agents.AI;

namespace Synaxis.InferenceGateway.Infrastructure.External.MicrosoftAgents.GithubCopilot;

public class GithubCopilotAgentClient : IChatClient, IDisposable
{
    private readonly AIAgent _agent;
    private readonly ILogger<GithubCopilotAgentClient>? _logger;
    private const string s_modelId = "copilot";
    private const string s_providerName = "GitHubCopilot";
    private readonly ChatClientMetadata _metadata = new ChatClientMetadata(s_providerName, new Uri("https://copilot.github.com/"), s_modelId);

    public GithubCopilotAgentClient(AIAgent agent, ILogger<GithubCopilotAgentClient>? logger = null)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _logger = logger;
    }

    public ChatClientMetadata Metadata => _metadata;

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var agentResponse = await _agent.RunAsync(messages, cancellationToken: cancellationToken).ConfigureAwait(false);

        // Try to extract messages from AgentResponse via common property names
        var respType = agentResponse?.GetType();
        ChatResponse chatResponse;

        if (respType != null)
        {
            var messagesProp = respType.GetProperty("Messages") ?? respType.GetProperty("Message") ?? respType.GetProperty("Result");
            if (messagesProp != null)
            {
                var val = messagesProp.GetValue(agentResponse);
                if (val is IEnumerable<ChatMessage> msgs)
                {
                    chatResponse = new ChatResponse(msgs.ToList());
                    chatResponse.ModelId = s_modelId;
                    chatResponse.AdditionalProperties ??= new AdditionalPropertiesDictionary();
                    chatResponse.AdditionalProperties["provider_name"] = s_providerName;
                    return chatResponse;
                }

                if (val is ChatMessage single)
                {
                    chatResponse = new ChatResponse(single);
                    chatResponse.ModelId = s_modelId;
                    chatResponse.AdditionalProperties ??= new AdditionalPropertiesDictionary();
                    chatResponse.AdditionalProperties["provider_name"] = s_providerName;
                    return chatResponse;
                }
            }
        }

        // Fallback: render agentResponse.ToString()
        var text = agentResponse?.ToString() ?? string.Empty;
        chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
        chatResponse.ModelId = s_modelId;
        chatResponse.AdditionalProperties ??= new AdditionalPropertiesDictionary();
        chatResponse.AdditionalProperties["provider_name"] = s_providerName;
        return chatResponse;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var agentUpdate in _agent.RunStreamingAsync(messages, cancellationToken: cancellationToken).WithCancellation(cancellationToken))
        {
            var chatUpdate = new ChatResponseUpdate
            {
                Role = agentUpdate.Role
            };

            // Copy AdditionalProperties if present
            try
            {
                var addProp = agentUpdate.GetType().GetProperty("AdditionalProperties");
                if (addProp != null)
                {
                    var ap = addProp.GetValue(agentUpdate) as AdditionalPropertiesDictionary;
                    if (ap != null) chatUpdate.AdditionalProperties = ap;
                }
            }
            catch { }

            // Copy FinishReason if present (string or enum)
            try
            {
                var finishProp = agentUpdate.GetType().GetProperty("FinishReason");
                if (finishProp != null)
                {
                    var fr = finishProp.GetValue(agentUpdate);
                    if (fr is string s)
                    {
                        if (Enum.TryParse<ChatFinishReason>(s, true, out var parsed))
                        {
                            chatUpdate.FinishReason = parsed;
                        }
                    }
                    else if (fr is ChatFinishReason cfr)
                    {
                        chatUpdate.FinishReason = cfr;
                    }
                }
            }
            catch { }

            // Map contents
            try
            {
                var contentsProp = agentUpdate.GetType().GetProperty("Contents");
                if (contentsProp != null)
                {
                    var contents = contentsProp.GetValue(agentUpdate) as System.Collections.IEnumerable;
                    if (contents != null)
                    {
                        foreach (var c in contents)
                        {
                            if (c is Microsoft.Extensions.AI.AIContent aiContent)
                            {
                                chatUpdate.Contents.Add(aiContent);
                                continue;
                            }

                            // fallback to string representation
                            var text = c?.ToString() ?? string.Empty;
                            chatUpdate.Contents.Add(new TextContent(text));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Failed to map agent contents to ChatResponseUpdate");
            }

            yield return chatUpdate;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == null) return null;
        return serviceType.IsInstanceOfType(_agent) ? _agent : null;
    }

    public void Dispose()
    {
        try
        {
            if (_agent is IAsyncDisposable asyncDisp)
            {
                asyncDisp.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            else if (_agent is IDisposable disp)
            {
                disp.Dispose();
            }
        }
        catch { }
    }
}
