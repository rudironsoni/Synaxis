using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Infrastructure.External.OpenAi;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

namespace Synaxis.InferenceGateway.Infrastructure.Jobs
{
    public class ProviderDiscoveryJob : IJob
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<ProviderDiscoveryJob> _logger;

        public ProviderDiscoveryJob(IServiceProvider provider, ILogger<ProviderDiscoveryJob> logger)
        {
            _provider = provider;
            _logger = logger;
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

                var endpoint = providerCfg.Endpoint ?? providerCfg.FallbackEndpoint ?? providerCfg.Key ?? string.Empty;
                var apiKey = providerCfg.Key ?? string.Empty;

                try
                {
                    var ct = CancellationToken.None;
                    var baseUrl = providerCfg.Endpoint ?? providerCfg.FallbackEndpoint ?? providerCfg.Endpoint ?? string.Empty;
                    var models = await discovery.GetModelsAsync(baseUrl, apiKey, ct).ConfigureAwait(false);

                    foreach (var found in models)
                    {
                        if (string.IsNullOrEmpty(found)) continue;

                        // Fuzzy match global model
                        var global = db.GlobalModels.FirstOrDefault(g => g.Id == found || (found.Contains(g.Id)));
                        if (global == null)
                        {
                            // Create minimal global model
                            global = new GlobalModel { Id = found, Name = found, Family = "unknown" };
                            db.GlobalModels.Add(global);
                        }

                        // Upsert ProviderModel
                        var existing = db.ProviderModels.FirstOrDefault(p => p.ProviderId == providerKey && p.ProviderSpecificId == found);
                        if (existing == null)
                        {
                            existing = new ProviderModel
                            {
                                ProviderId = providerKey,
                                ProviderSpecificId = found,
                                GlobalModelId = global.Id,
                                IsAvailable = true
                            };
                            db.ProviderModels.Add(existing);
                        }
                        else
                        {
                            existing.IsAvailable = true;
                            existing.GlobalModelId = global.Id;
                        }
                    }

                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error discovering models for provider {Provider}", providerKey);
                }
            }
        }
    }
}
