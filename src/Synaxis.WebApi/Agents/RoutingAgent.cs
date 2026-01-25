using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synaxis.Application.Routing;
using Synaxis.WebApi.Middleware;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.WebApi.Agents;

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
        var httpContext = _httpContextAccessor.HttpContext;

        var modelId = await GetModelIdAsync(httpContext, cancellationToken) ?? "default";
        var caps = new RequiredCapabilities { Streaming = false }; // Add other caps extraction logic if possible

        var resolution = await resolver.ResolveAsync(modelId, EndpointKind.ChatCompletions, caps);
        
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

        var response = await chatClient.GetResponseAsync(messages, chatOptions, cancellationToken);

        return new AgentResponse(new ChatMessage(ChatRole.Assistant, response.Text));
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
        var httpContext = _httpContextAccessor.HttpContext;

        var modelId = await GetModelIdAsync(httpContext, cancellationToken) ?? "default";
        var caps = new RequiredCapabilities { Streaming = true };

        var resolution = await resolver.ResolveAsync(modelId, EndpointKind.ChatCompletions, caps);
        
        if (resolution.Candidates.Count == 0)
        {
            throw new ArgumentException($"No providers available for model '{modelId}' with requested capabilities.");
        }

        var chatOptions = new ChatOptions { ModelId = resolution.CanonicalId.ToString() };

        if (httpContext != null)
        {
            httpContext.Items["RoutingContext"] = new RoutingContext(modelId, resolution.CanonicalId.ToString(), resolution.CanonicalId.Provider);
        }

        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, chatOptions, cancellationToken))
        {
            yield return new AgentResponseUpdate(update);
        }
    }

    public override ValueTask<AgentThread> GetNewThreadAsync(CancellationToken cancellationToken = default)
        => new ValueTask<AgentThread>(new RoutingAgentThread());

    public override ValueTask<AgentThread> DeserializeThreadAsync(JsonElement serializedThread, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        => new ValueTask<AgentThread>(new RoutingAgentThread());

    private async Task<string?> GetModelIdAsync(HttpContext? context, CancellationToken cancellationToken)
    {
        if (context == null) return null;

        try
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync(cancellationToken);
            context.Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body)) return null;

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("model", out var modelElement))
            {
                return modelElement.GetString();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }
}
