// <copyright file="ProviderDiscoveryJob.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Jobs
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Quartz;
    using Synaxis.InferenceGateway.Application.Configuration;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
    using Synaxis.InferenceGateway.Infrastructure.External.OpenAi;

    public class ProviderDiscoveryJob : IJob
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<ProviderDiscoveryJob> _logger;

        public ProviderDiscoveryJob(IServiceProvider provider, ILogger<ProviderDiscoveryJob> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        private static string GetEffectiveBaseUrl(ProviderConfig config, string providerKey)
        {
            if (config == null) return string.Empty;

            // Prefer explicit endpoint if provided
            var url = config.Endpoint;

            // If no explicit endpoint, allow fallback endpoint before using type defaults
            if (string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(config.FallbackEndpoint))
            {
                url = config.FallbackEndpoint;
            }

            // If still empty, choose defaults based on provider type
            if (string.IsNullOrWhiteSpace(url))
            {
                var type = config.Type ?? string.Empty;
                switch (type.Trim().ToLowerInvariant())
                {
                    case "nvidia":
                        url = "https://integrate.api.nvidia.com";
                        break;
                    case "huggingface":
                        url = "https://router.huggingface.co";
                        break;
                    case "groq":
                        url = "https://api.groq.com/openai";
                        break;
                    case "openrouter":
                        url = "https://openrouter.ai/api";
                        break;
                    case "deepseek":
                        url = "https://api.deepseek.com";
                        break;
                    case "openai":
                        url = "https://api.openai.com";
                        break;
                    case "cohere":
                        url = "https://api.cohere.com/v2";
                        break;
                    case "duckduckgo":
                        url = "https://duckduckgo.com/duckchat/v1";
                        break;
                    case "aihorde":
                        url = "https://stablehorde.net/api/v2";
                        break;
                    case "siliconflow":
                        url = "https://api.siliconflow.cn/v1";
                        break;
                    case "sambanova":
                        url = "https://api.sambanova.ai/v1";
                        break;
                    case "zai":
                        url = "https://open.bigmodel.cn/api/paas/v4";
                        break;
                    case "hyperbolic":
                        url = "https://api.hyperbolic.xyz/v1";
                        break;
                    case "githubmodels":
                        url = "https://models.inference.ai.azure.com";
                        break;
                    case "pollinations":
                        url = "https://text.pollinations.ai";
                        break;
                    case "kilocode":
                        url = "https://api.kilocode.ai";
                        break;
                    default:
                        // Unknown type and no endpoint provided
                        return string.Empty;
                }
            }

            // Clean up trailing /v1 if present. Many discovery clients append /v1/models.
            url = url.Trim();
            url = url.TrimEnd('/');
            if (url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                url = url.Substring(0, url.Length - "/v1".Length);
            }

            return url;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = _provider.CreateScope();
            var config = scope.ServiceProvider.GetRequiredService<IOptions<SynaxisConfiguration>>().Value;
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var discovery = scope.ServiceProvider.GetRequiredService<IOpenAiModelDiscoveryClient>();

            foreach (var kv in config.Providers)
            {
                var providerKey = kv.Key;
                var providerCfg = kv.Value;

                if (!providerCfg.Enabled) continue;

                // Skip antigravity for now
                if (string.Equals(providerCfg.Type, "antigravity", StringComparison.OrdinalIgnoreCase)) continue;

                var apiKey = providerCfg.Key ?? string.Empty;

                try
                {
                    var ct = CancellationToken.None;
                    var baseUrl = GetEffectiveBaseUrl(providerCfg, providerKey);
                    if (string.IsNullOrWhiteSpace(baseUrl))
                    {
                        _logger.LogWarning("No effective base URL could be determined for provider {Provider}", providerKey);
                        continue;
                    }

                    var models = await discovery.GetModelsAsync(baseUrl, apiKey, ct).ConfigureAwait(false);
                    var discoveredModels = models?.ToList() ?? new System.Collections.Generic.List<string>();

                    foreach (var found in discoveredModels)
                    {
                        if (string.IsNullOrEmpty(found)) continue;

                        // Fuzzy match global model
                        var global = db.GlobalModels.FirstOrDefault(g => g.Id == found || (found.Contains(g.Id)));
                        if (global == null)
                        {
                            // Create minimal global model
                            global = new GlobalModel { Id = found, Name = found, Family = "unknown" };
                            db.GlobalModels.Add(global);
                            try
                            {
                                await db.SaveChangesAsync().ConfigureAwait(false);
                            }
                            catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
                            {
                                db.ChangeTracker.Clear();
                                var reloadedGlobal = db.GlobalModels.FirstOrDefault(g => g.Id == found);
                                if (reloadedGlobal != null)
                                {
                                    global = reloadedGlobal;
                                }
                            }
                        }

                        // Upsert ProviderModel
                        var existing = db.ProviderModels.FirstOrDefault(p => p.ProviderId == providerKey && p.ProviderSpecificId == found);
                        if (existing == null)
                        {
                            try
                            {
                                existing = new ProviderModel
                                {
                                    ProviderId = providerKey,
                                    ProviderSpecificId = found,
                                    GlobalModelId = global.Id,
                                    IsAvailable = true
                                };
                                db.ProviderModels.Add(existing);
                                await db.SaveChangesAsync().ConfigureAwait(false);
                            }
                            catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
                            {
                                db.ChangeTracker.Clear();
                                var reloadedExisting = db.ProviderModels.FirstOrDefault(p => p.ProviderId == providerKey && p.ProviderSpecificId == found);
                                if (reloadedExisting != null)
                                {
                                    reloadedExisting.IsAvailable = true;
                                    reloadedExisting.GlobalModelId = global.Id;
                                    await db.SaveChangesAsync().ConfigureAwait(false);
                                }
                            }
                        }
                        else
                        {
                            existing.IsAvailable = true;
                            existing.GlobalModelId = global.Id;
                            try
                            {
                                await db.SaveChangesAsync().ConfigureAwait(false);
                            }
                            catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
                            {
                                db.ChangeTracker.Clear();
                            }
                        }
                    }

                    _logger.LogInformation("ProviderDiscoveryJob: Successfully upserted {Count} models for provider {Provider}", discoveredModels.Count, providerKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error discovering models for provider {Provider}", providerKey);
                }
            }
        }
    }
}
