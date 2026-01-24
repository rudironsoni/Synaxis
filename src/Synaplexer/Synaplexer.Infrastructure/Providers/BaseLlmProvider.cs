using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaplexer.Domain.Interfaces;
using Synaplexer.Domain.ValueObjects;
using Synaplexer.Infrastructure.Configuration;

namespace Synaplexer.Infrastructure.Providers;

public abstract class BaseLlmProvider : ILlmProvider
{
    protected readonly HttpClient Http;
    protected readonly ILogger Logger;
    protected readonly ProviderConfiguration Configuration;

    protected BaseLlmProvider(HttpClient http, ILogger logger, IOptionsSnapshot<ProvidersOptions> options, string providerName)
    {
        Http = http;
        Http.Timeout = TimeSpan.FromSeconds(120);
        Logger = logger;
        Configuration = options.Value.TryGetValue(providerName, out var config)
            ? config
            : new ProviderConfiguration();
    }

    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract ProviderTier Tier { get; }
    public int Priority => Configuration.Priority;

    public abstract bool SupportsModel(string modelId);
    public abstract Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default);
    public abstract IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, CancellationToken ct = default);

    protected string? GetApiKey()
    {
        if (Configuration.ApiKeys == null || Configuration.ApiKeys.Count == 0)
            return null;

        var index = Random.Shared.Next(Configuration.ApiKeys.Count);
        return Configuration.ApiKeys[index];
    }
}
