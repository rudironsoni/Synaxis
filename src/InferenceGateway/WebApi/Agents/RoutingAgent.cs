using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.Application.Translation;
using Synaxis.InferenceGateway.WebApi.Middleware;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.WebApi.Agents;

public class RoutingAgentThread : AgentThread { }

public class RoutingAgent : AIAgent
{
    public override string Name => "Synaxis";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RoutingAgent> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RoutingAgent(IServiceScopeFactory scopeFactory, ILogger<RoutingAgent> logger, IHttpContextAccessor httpContextAccessor)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
        var resolver = scope.ServiceProvider.GetRequiredService<IModelResolver>();
        var translator = scope.ServiceProvider.GetRequiredService<ITranslationPipeline>();
        var httpContext = _httpContextAccessor.HttpContext;

        var openAIRequest = await ParseOpenAIRequestAsync(httpContext, cancellationToken);
        var modelId = !string.IsNullOrWhiteSpace(openAIRequest?.Model) ? openAIRequest.Model : "default";

        var canonicalRequest = new CanonicalRequest(
            EndpointKind.ChatCompletions,
            modelId,
            messages.ToList(),
            Tools: MapTools(openAIRequest?.Tools),
            ToolChoice: openAIRequest?.ToolChoice,
            ResponseFormat: openAIRequest?.ResponseFormat,
            AdditionalOptions: new ChatOptions
            {
                Temperature = (float?)openAIRequest?.Temperature,
                TopP = (float?)openAIRequest?.TopP,
                MaxOutputTokens = openAIRequest?.MaxTokens,
                StopSequences = MapStopSequences(openAIRequest?.Stop)
            });

        var translatedRequest = translator.TranslateRequest(canonicalRequest);
        var caps = new RequiredCapabilities { Streaming = false }; // Add other caps extraction logic if possible

        Guid? tenantId = null;
        if (httpContext?.User?.FindFirst("tenantId")?.Value is string tenantIdStr && Guid.TryParse(tenantIdStr, out var tid))
        {
            tenantId = tid;
        }

        var resolution = await resolver.ResolveAsync(translatedRequest.Model, EndpointKind.ChatCompletions, caps, tenantId);

        if (resolution.Candidates.Count == 0)
        {
            throw new ArgumentException($"No providers available for model '{modelId}' with requested capabilities.");
        }

        if (httpContext != null)
        {
            httpContext.Items["RoutingContext"] = new RoutingContext(modelId, resolution.CanonicalId.ToString(), resolution.CanonicalId.Provider);
        }

        // Update options with resolved model if needed, or pass to chatClient via ChatOptions
        var chatOptions = new ChatOptions { ModelId = resolution.CanonicalId.ToString() };
        // Map other options...

        var response = await chatClient.GetResponseAsync(translatedRequest.Messages, chatOptions, cancellationToken);
        var message = response.Messages.FirstOrDefault() ?? new ChatMessage(ChatRole.Assistant, "");
        var toolCalls = message.Contents.OfType<FunctionCallContent>().ToList();
        var canonicalResponse = new CanonicalResponse(message.Text, toolCalls);
        var translatedResponse = translator.TranslateResponse(canonicalResponse);

        var agentMessage = new ChatMessage(ChatRole.Assistant, translatedResponse.Content);
        if (translatedResponse.ToolCalls != null)
        {
            foreach (var toolCall in translatedResponse.ToolCalls)
            {
                agentMessage.Contents.Add(toolCall);
            }
        }

        return new AgentResponse(agentMessage);
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();
        var resolver = scope.ServiceProvider.GetRequiredService<IModelResolver>();
        var translator = scope.ServiceProvider.GetRequiredService<ITranslationPipeline>();
        var httpContext = _httpContextAccessor.HttpContext;

        var openAIRequest = await ParseOpenAIRequestAsync(httpContext, cancellationToken);
        var modelId = !string.IsNullOrWhiteSpace(openAIRequest?.Model) ? openAIRequest.Model : "default";

        var canonicalRequest = new CanonicalRequest(
            EndpointKind.ChatCompletions,
            modelId,
            messages.ToList(),
            Tools: MapTools(openAIRequest?.Tools),
            ToolChoice: openAIRequest?.ToolChoice,
            ResponseFormat: openAIRequest?.ResponseFormat,
            AdditionalOptions: new ChatOptions
            {
                Temperature = (float?)openAIRequest?.Temperature,
                TopP = (float?)openAIRequest?.TopP,
                MaxOutputTokens = openAIRequest?.MaxTokens,
                StopSequences = MapStopSequences(openAIRequest?.Stop)
            });

        var translatedRequest = translator.TranslateRequest(canonicalRequest);
        var caps = new RequiredCapabilities { Streaming = true };

        Guid? tenantId = null;
        if (httpContext?.User?.FindFirst("tenantId")?.Value is string tenantIdStr && Guid.TryParse(tenantIdStr, out var tid))
        {
            tenantId = tid;
        }

        var resolution = await resolver.ResolveAsync(translatedRequest.Model, EndpointKind.ChatCompletions, caps, tenantId);

        if (resolution.Candidates.Count == 0)
        {
            throw new ArgumentException($"No providers available for model '{modelId}' with requested capabilities.");
        }

        var chatOptions = new ChatOptions { ModelId = resolution.CanonicalId.ToString() };

        if (httpContext != null)
        {
            httpContext.Items["RoutingContext"] = new RoutingContext(modelId, resolution.CanonicalId.ToString(), resolution.CanonicalId.Provider);
        }

        await foreach (var update in chatClient.GetStreamingResponseAsync(translatedRequest.Messages, chatOptions, cancellationToken))
        {
            var translatedUpdate = translator.TranslateUpdate(update);
            yield return new AgentResponseUpdate(translatedUpdate);
        }
    }

    public override ValueTask<AgentThread> GetNewThreadAsync(CancellationToken cancellationToken = default)
        => new ValueTask<AgentThread>(new RoutingAgentThread());

    public override ValueTask<AgentThread> DeserializeThreadAsync(JsonElement serializedThread, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        => new ValueTask<AgentThread>(new RoutingAgentThread());



    private async Task<OpenAIRequest?> ParseOpenAIRequestAsync(HttpContext? context, CancellationToken cancellationToken)
    {
        if (context == null) return null;

        try
        {
            context.Request.EnableBuffering();
            context.Request.Body.Position = 0;
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync(cancellationToken);
            context.Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body)) return null;

            return JsonSerializer.Deserialize<OpenAIRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private IList<AITool>? MapTools(List<OpenAITool>? tools)
    {
        if (tools == null) return null;
        var result = new List<AITool>();
        foreach (var tool in tools)
        {
            if (tool.Type == "function" && tool.Function != null)
            {
                var function = AIFunctionFactory.Create(
                    (string args) => Task.CompletedTask, // Dummy delegate, we just need the metadata
                    tool.Function.Name,
                    tool.Function.Description);
                // Note: AIFunctionFactory is tricky for just metadata.
                // Better to use a custom AITool implementation or just pass the raw object if supported.
                // For now, we'll skip complex mapping as Microsoft.Extensions.AI handles tools differently.
                // We might need to pass the raw tools in AdditionalOptions if the ChatClient supports it.
            }
        }
        return null; // Placeholder
    }

    private IList<string>? MapStopSequences(object? stop)
    {
        if (stop is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return new List<string> { element.GetString()! };
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        list.Add(item.GetString()!);
                    }
                }
                return list;
            }
        }
        return null;
    }
}