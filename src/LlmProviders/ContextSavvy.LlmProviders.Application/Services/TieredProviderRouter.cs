using System.Diagnostics;
using ContextSavvy.Core.Metrics;
using ***REMOVED***.Priority;
using ContextSavvy.LlmProviders.Application.Commands;
using AppChatCompletionResult = ContextSavvy.LlmProviders.Application.Dtos.ChatCompletionResult;
using DomainChatCompletionResult = ContextSavvy.LlmProviders.Domain.ValueObjects.ChatCompletionResult;
using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ContextSavvy.LlmProviders.Application.Services;

/// <summary>
/// Routes LLM requests to the best available provider based on cost, tier, and capabilities.
/// Implements Ultra-Miser Mode: prefer Tier 1 (free/fast) providers, then escalate as needed.
/// </summary>
public class TieredProviderRouter : ITieredProviderRouter, IProviderService
{
    private readonly IEnumerable<ILlmProvider> _providers;
    private readonly ILogger<TieredProviderRouter> _logger;
    private readonly IMetricsCollector _metricsCollector;
    private readonly UsageTracker _usageTracker;

    public string Name => "TieredProviderRouter";
    public ProviderTier Tier => ProviderTier.Tier1_FreeFast;

    public TieredProviderRouter(
        IEnumerable<ILlmProvider> providers,
        ILogger<TieredProviderRouter> logger,
        IMetricsCollector metricsCollector,
        UsageTracker usageTracker)
    {
        _providers = providers;
        _logger = logger;
        _metricsCollector = metricsCollector;
        _usageTracker = usageTracker;
    }

    public async Task<AppChatCompletionResult> RouteAsync(ChatCompletionCommand command, CancellationToken cancellationToken = default)
    {
        return await CompleteAsync(command, cancellationToken);
    }

    public async Task<AppChatCompletionResult> CompleteAsync(ChatCompletionCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Routing chat completion request for model {Model} (Ultra-Miser Mode)", command.Model);

        var eligibleProviders = _providers
            .Where(p => p.SupportsModel(command.Model))
            .OrderBy(p => p.Tier)
            .ToList();

        if (!eligibleProviders.Any())
        {
            _logger.LogWarning("No providers available for model {Model}", command.Model);
            throw new InvalidOperationException($"No providers found for model {command.Model}");
        }

        var exceptions = new List<Exception>();
        var domainRequest = MapToDomainRequest(command);

        foreach (var provider in eligibleProviders)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Attempting chat completion with provider {Provider} (Tier: {Tier})", provider.Name, provider.Tier);
                
                var result = await provider.ChatAsync(domainRequest, cancellationToken);
                
                stopwatch.Stop();
                RecordSuccess(provider, command.Model, result, stopwatch.Elapsed);

                return MapToApplicationResult(result, command.Model);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "Provider {Provider} failed for model {Model}", provider.Name, command.Model);
                
                RecordFailure(provider, command.Model, ex.Message, stopwatch.Elapsed);
                exceptions.Add(ex);
            }
        }

        _logger.LogError("All providers failed for model {Model}. Total attempts: {Attempts}", command.Model, eligibleProviders.Count);
        throw new AggregateException($"All providers failed for model {command.Model}", exceptions);
    }

    public bool SupportsModel(string model)
    {
        return _providers.Any(p => p.SupportsModel(model));
    }

    private static ChatRequest MapToDomainRequest(ChatCompletionCommand command)
    {
        return new ChatRequest(
            command.Model,
            command.Messages.Select(m => new Message(m.Role, m.Content)).ToList(),
            (double)command.Temperature,
            command.MaxTokens
        );
    }

    private static AppChatCompletionResult MapToApplicationResult(DomainChatCompletionResult result, string model)
    {
        return new AppChatCompletionResult(
            result.Content,
            model,
            result.Usage.TotalTokens,
            result.FinishReason
        );
    }

    private void RecordSuccess(ILlmProvider provider, string model, DomainChatCompletionResult result, TimeSpan duration)
    {
        var method = MapToAccessMethod(provider.Tier);
        _metricsCollector.RecordSuccess(provider.Name, method, duration);
        
        _usageTracker.RecordRequest(
            provider.Name,
            model,
            result.Usage.PromptTokens,
            result.Usage.CompletionTokens,
            success: true,
            latency: duration
        );
    }

    private void RecordFailure(ILlmProvider provider, string model, string error, TimeSpan duration)
    {
        var method = MapToAccessMethod(provider.Tier);
        _metricsCollector.RecordFailure(provider.Name, method, error, duration);
        
        _usageTracker.RecordRequest(
            provider.Name,
            model,
            0,
            0,
            success: false,
            latency: duration
        );
    }

    private static AccessMethodType MapToAccessMethod(ProviderTier tier)
    {
        return tier switch
        {
            ProviderTier.Tier3_Ghost => AccessMethodType.BrowserCookieAuth,
            _ => AccessMethodType.ApiDirect
        };
    }
}

