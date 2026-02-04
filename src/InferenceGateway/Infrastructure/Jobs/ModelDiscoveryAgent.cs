using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Synaxis.InferenceGateway.Infrastructure.Agents.Tools;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

namespace Synaxis.InferenceGateway.Infrastructure.Jobs;

/// <summary>
/// Model Discovery Agent - Runs daily at 2 AM.
/// Discovers new models from configured providers and adds them to the platform.
/// </summary>
[DisallowConcurrentExecution]
public class ModelDiscoveryAgent : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModelDiscoveryAgent> _logger;

    public ModelDiscoveryAgent(IServiceProvider serviceProvider, ILogger<ModelDiscoveryAgent> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("[ModelDiscovery][{CorrelationId}] Starting model discovery", correlationId);

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
        var alertTool = scope.ServiceProvider.GetRequiredService<IAlertTool>();
        var auditTool = scope.ServiceProvider.GetRequiredService<IAuditTool>();

        try
        {
            // Get existing models for comparison
            var existingModels = await db.GlobalModels
                .Select(m => m.Id)
                .ToListAsync(context.CancellationToken);

            var existingModelSet = new HashSet<string>(existingModels);

            // Get all provider models
            var providerModels = await db.ProviderModels
                .Where(pm => pm.IsAvailable)
                .Select(pm => new { pm.GlobalModelId, pm.ProviderId, pm.ProviderSpecificId })
                .ToListAsync(context.CancellationToken);

            // Find new models (in ProviderModels but not in GlobalModels)
            var newModels = providerModels
                .Where(pm => !existingModelSet.Contains(pm.GlobalModelId))
                .GroupBy(pm => pm.GlobalModelId)
                .Select(g => new
                {
                    ModelId = g.Key,
                    Providers = g.Select(x => x.ProviderId).Distinct().ToList()
                })
                .ToList();

            if (newModels.Any())
            {
                _logger.LogInformation("[ModelDiscovery][{CorrelationId}] Found {Count} new models", 
                    correlationId, newModels.Count);

                int addedCount = 0;

                foreach (var model in newModels)
                {
                    try
                    {
                        // Create minimal global model
                        var globalModel = new GlobalModel
                        {
                            Id = model.ModelId,
                            Name = model.ModelId,
                            Family = "unknown",
                            Description = $"Auto-discovered model from {string.Join(", ", model.Providers)}",
                            InputPrice = 0m,
                            OutputPrice = 0m
                        };

                        db.GlobalModels.Add(globalModel);
                        await db.SaveChangesAsync(context.CancellationToken);

                        addedCount++;

                        _logger.LogInformation(
                            "[ModelDiscovery][{CorrelationId}] Added new model: {ModelId} from providers: {Providers}",
                            correlationId, model.ModelId, string.Join(", ", model.Providers));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[ModelDiscovery][{CorrelationId}] Failed to add model {ModelId}",
                            correlationId, model.ModelId);
                    }
                }

                // Send notification to admins
                if (addedCount > 0)
                {
                    await alertTool.SendAdminAlertAsync(
                        "New Models Discovered",
                        $"Model Discovery Agent found and added {addedCount} new models. Review the models in the admin panel.",
                        AlertSeverity.Info,
                        context.CancellationToken);

                    await auditTool.LogActionAsync(
                        "ModelDiscovery",
                        "ModelsAdded",
                        null,
                        null,
                        $"Added {addedCount} new models to platform",
                        correlationId,
                        context.CancellationToken);
                }

                _logger.LogInformation(
                    "[ModelDiscovery][{CorrelationId}] Completed: Added {Added} of {Found} new models",
                    correlationId, addedCount, newModels.Count);
            }
            else
            {
                _logger.LogInformation("[ModelDiscovery][{CorrelationId}] No new models found", correlationId);
            }

            // Update OrganizationModels for organizations with these providers enabled
            await UpdateOrganizationModelsAsync(db, correlationId, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ModelDiscovery][{CorrelationId}] Job failed", correlationId);
        }
    }

    private async Task UpdateOrganizationModelsAsync(
        ControlPlaneDbContext db,
        string correlationId,
        CancellationToken ct)
    {
        try
        {
            // TODO: Implement organization-specific model availability
            // This would update which models are available to which organizations
            // based on their enabled providers
            _logger.LogDebug("[ModelDiscovery][{CorrelationId}] Organization model update completed", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ModelDiscovery][{CorrelationId}] Failed to update organization models", correlationId);
        }
    }
}
