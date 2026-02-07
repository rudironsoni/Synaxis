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
    using Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.Dto;

    /// <summary>
    /// ModelsDevSyncJob class.
    /// </summary>
    public class ModelsDevSyncJob : IJob
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<ModelsDevSyncJob> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelsDevSyncJob"/> class.
        /// </summary>
        /// <param name="provider">The service provider.</param>
        /// <param name="logger">The logger.</param>
        public ModelsDevSyncJob(IServiceProvider provider, ILogger<ModelsDevSyncJob> logger)
        {
            this._provider = provider;
            this._logger = logger;
        }

        private static string Truncate(string? value, int maxLength)
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

        /// <inheritdoc/>
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
                if (string.IsNullOrEmpty(m.Id))
                {
                    continue;
                }

                var existing = await db.GlobalModels.FindAsync(new object[] { m.Id }, ct).ConfigureAwait(false);
                if (existing == null)
                {
                    existing = new GlobalModel { Id = m.Id! };
                    db.GlobalModels.Add(existing);
                }

                UpdateGlobalModel(existing, m);
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

        private static void UpdateGlobalModel(GlobalModel existing, ModelDto m)
        {
            existing.Name = Truncate(m.Name ?? m.Id!, 200);
            existing.Family = Truncate(m.Family ?? "unknown", 200);

            existing.ContextWindow = m.Limit?.Context ?? existing.ContextWindow;
            existing.MaxOutputTokens = m.Limit?.Output ?? existing.MaxOutputTokens;

            existing.InputPrice = m.Cost?.Input ?? existing.InputPrice;
            existing.OutputPrice = m.Cost?.Output ?? existing.OutputPrice;

            existing.IsOpenWeights = m.OpenWeights ?? existing.IsOpenWeights;

            // Capabilities
            existing.SupportsTools = m.ToolCall ?? existing.SupportsTools;
            existing.SupportsReasoning = m.Reasoning ?? existing.SupportsReasoning;
            existing.SupportsStructuredOutput = m.StructuredOutput ?? existing.SupportsStructuredOutput;

            var inputs = m.Modalities?.Input ?? Array.Empty<string>();
            existing.SupportsVision = inputs.Contains("image", StringComparer.OrdinalIgnoreCase);
            existing.SupportsAudio = inputs.Contains("audio", StringComparer.OrdinalIgnoreCase);

            // Release date parsing - ensure stored DateTime is UTC so PostgreSQL 'timestamptz' accepts it
            if (!string.IsNullOrEmpty(m.ReleaseDate) && DateTime.TryParse(m.ReleaseDate, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
            {
                existing.ReleaseDate = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
        }
    }
}
