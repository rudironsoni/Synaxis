using System.Diagnostics;
using Synaplexer.Core.Metrics;
using Synaplexer.Application.Commands;
using AppChatCompletionResult = Synaplexer.Application.Dtos.ChatCompletionResult;
using DomainChatCompletionResult = Synaplexer.Domain.ValueObjects.ChatCompletionResult;
using Synaplexer.Domain.Interfaces;
using Synaplexer.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Synaplexer.Application.Services;

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

        var priorityGroups = _providers
            .Where(p => p.SupportsModel(command.Model))
            .GroupBy(p => p.Priority)
            .OrderBy(g => g.Key)
            .ToList();

        if (!priorityGroups.Any())
        {
            _logger.LogWarning("No providers available for model {Model}", command.Model);
            throw new InvalidOperationException($"No providers found for model {command.Model}");
        }

        var exceptions = new List<Exception>();
        var domainRequest = MapToDomainRequest(command);
        var random = new Random();

        foreach (var group in priorityGroups)
        {
            var shuffledProviders = group.OrderBy(_ => random.Next()).ToList();
            
            foreach (var provider in shuffledProviders)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    _logger.LogInformation("Attempting chat completion with provider {Provider} (Priority: {Priority})", provider.Name, provider.Priority);
                    
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
        }

        _logger.LogError("All providers failed for model {Model}. Total attempts: {Attempts}", command.Model, exceptions.Count);
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
        return AccessMethodType.ApiDirect;
    }
}

