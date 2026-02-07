// <copyright file="ModelsDevSyncJob.cs" company="PlaceholderCompany">
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
    using Quartz;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
    using Synaxis.InferenceGateway.Infrastructure.External.ModelsDev;

    public class ModelsDevSyncJob : IJob
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<ModelsDevSyncJob> _logger;

        public ModelsDevSyncJob(IServiceProvider provider, ILogger<ModelsDevSyncJob> logger)
        {
            this._provider = provider;
            this._logger = logger;
        }

        private string Truncate(string? value, int maxLength)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = this._provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IModelsDevClient>();
            var ct = CancellationToken.None;

            var models = await client.GetAllModelsAsync(ct).ConfigureAwait(false);
            if (models == null || models.Count == 0)
            {
                return;
            }

            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();

            foreach (var m in models)
            {
                if (string.IsNullOrEmpty(m.id))
                {
                    continue;
                }

                var existing = await db.GlobalModels.FindAsync(new object[] { m.id }, ct).ConfigureAwait(false);
                if (existing == null)
                {
                    existing = new GlobalModel { Id = m.id! };
                    db.GlobalModels.Add(existing);
                }

                existing.Name = this.Truncate(m.name ?? m.id, 200);
                existing.Family = this.Truncate(m.family ?? "unknown", 200);

                existing.ContextWindow = m.limit?.context ?? existing.ContextWindow;
                existing.MaxOutputTokens = m.limit?.output ?? existing.MaxOutputTokens;

                existing.InputPrice = m.cost?.input ?? existing.InputPrice;
                existing.OutputPrice = m.cost?.output ?? existing.OutputPrice;

                existing.IsOpenWeights = m.open_weights ?? existing.IsOpenWeights;

                // Capabilities
                existing.SupportsTools = m.tool_call ?? existing.SupportsTools;
                existing.SupportsReasoning = m.reasoning ?? existing.SupportsReasoning;
                existing.SupportsStructuredOutput = m.structured_output ?? existing.SupportsStructuredOutput;

                var inputs = m.modalities?.input ?? Array.Empty<string>();
                existing.SupportsVision = inputs.Contains("image", StringComparer.OrdinalIgnoreCase);
                existing.SupportsAudio = inputs.Contains("audio", StringComparer.OrdinalIgnoreCase);

                // Release date parsing - ensure stored DateTime is UTC so PostgreSQL 'timestamptz' accepts it
                if (!string.IsNullOrEmpty(m.release_date))
                {
                    if (DateTime.TryParse(m.release_date, out var dt))
                    {
                        existing.ReleaseDate = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    }
                }
            }

            try
            {
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                this._logger.LogInformation("ModelsDevSyncJob: synced {Count} models", models.Count);
                this._logger.LogInformation("ModelsDevSyncJob: Successfully upserted {Count} global models", models.Count);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "ModelsDevSyncJob: failed to save changes: {Message}", ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
