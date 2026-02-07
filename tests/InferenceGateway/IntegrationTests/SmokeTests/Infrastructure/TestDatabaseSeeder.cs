using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using System.Linq;
using System.Collections.Generic;

namespace Tests.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;

public static class TestDatabaseSeeder
{
    // Seeds GlobalModel and ProviderModel based on configuration.
    // Expects configuration structure:
    // Synaxis:InferenceGateway:Providers:{providerKey}:Enabled (bool)
    // Synaxis:InferenceGateway:Providers:{providerKey}:Models (array of strings)
    // Synaxis:InferenceGateway:CanonicalModels:{providerKey}:{modelName} = canonicalId (optional mapping)
    public static async Task SeedAsync(ControlPlaneDbContext context, IConfiguration config)
    {
        var providersSection = config.GetSection("Synaxis:InferenceGateway:Providers");
        if (!providersSection.Exists())
        {
            return;
        }

        var providerSections = providersSection.GetChildren().ToList();

        // First loop: ensure all GlobalModels exist/are updated
        var processedGlobalIds = new HashSet<string>();
        foreach (var providerSection in providerSections)
        {
            var providerKey = providerSection.Key;
            var enabled = providerSection.GetValue<bool>("Enabled");
            if (!enabled)
            {
                continue;
            }

            var models = providerSection.GetSection("Models").Get<string[]>() ?? new string[0];

            foreach (var modelName in models)
            {
                // determine canonical id
                var canonicalMapping = config.GetSection("Synaxis:InferenceGateway:CanonicalModels");
                string canonicalId = modelName;
                if (canonicalMapping.Exists())
                {
                    var providerMap = canonicalMapping.GetSection(providerKey);
                    if (providerMap.Exists())
                    {
                        var mapped = providerMap.GetValue<string>(modelName);
                        if (!string.IsNullOrEmpty(mapped))
                        {
                            canonicalId = mapped;
                        }
                    }
                }

                if (processedGlobalIds.Contains(canonicalId))
                {
                    continue;
                }

                // Upsert GlobalModel
                var global = await context.GlobalModels.FindAsync(canonicalId);
                if (global == null)
                {
                    global = new GlobalModel
                    {
                        Id = canonicalId,
                        Name = canonicalId,
                        Family = "test-seeded",
                        InputPrice = 0m,
                        OutputPrice = 0m,
                    };
                    await context.GlobalModels.AddAsync(global);
                }
                else
                {
                    // update fields if necessary
                    global.Name = canonicalId;
                    global.Family = "test-seeded";
                    global.InputPrice = 0m;
                    global.OutputPrice = 0m;
                    context.GlobalModels.Update(global);
                }

                processedGlobalIds.Add(canonicalId);
            }
        }

        // Persist GlobalModels so FK references are valid in next phase
        await context.SaveChangesAsync();

        // Second loop: upsert ProviderModels now that GlobalModels exist
        foreach (var providerSection in providerSections)
        {
            var providerKey = providerSection.Key;
            var enabled = providerSection.GetValue<bool>("Enabled");
            if (!enabled)
            {
                continue;
            }

            var models = providerSection.GetSection("Models").Get<string[]>() ?? new string[0];

            foreach (var modelName in models)
            {
                // determine canonical id
                var canonicalMapping = config.GetSection("Synaxis:InferenceGateway:CanonicalModels");
                string canonicalId = modelName;
                if (canonicalMapping.Exists())
                {
                    var providerMap = canonicalMapping.GetSection(providerKey);
                    if (providerMap.Exists())
                    {
                        var mapped = providerMap.GetValue<string>(modelName);
                        if (!string.IsNullOrEmpty(mapped))
                        {
                            canonicalId = mapped;
                        }
                    }
                }

                // Upsert ProviderModel
                var providerModel = await context.ProviderModels
                    .FirstOrDefaultAsync(pm => pm.ProviderId == providerKey && pm.ProviderSpecificId == modelName);

                if (providerModel == null)
                {
                    providerModel = new ProviderModel
                    {
                        ProviderId = providerKey,
                        GlobalModelId = canonicalId,
                        ProviderSpecificId = modelName,
                        IsAvailable = true,
                    };
                    await context.ProviderModels.AddAsync(providerModel);
                }
                else
                {
                    providerModel.IsAvailable = true;
                    providerModel.GlobalModelId = canonicalId;
                    context.ProviderModels.Update(providerModel);
                }
            }
        }

        await context.SaveChangesAsync();
    }
}
