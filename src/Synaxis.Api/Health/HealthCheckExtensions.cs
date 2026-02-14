using Microsoft.Extensions.Diagnostics.HealthChecks;
using Synaxis.Api.Health.Layers;

namespace Synaxis.Api.Health;

/// <summary>
/// 4-Layer Health Model for Synaxis
/// Layer 1: Infrastructure (K8s probes)
/// Layer 2: Dependencies (DB, Redis, Service Bus checks)
/// Layer 3: Application (custom health metrics)
/// Layer 4: Business (SLO compliance)
/// </summary>
public static class HealthCheckExtensions
{
    public static IHealthChecksBuilder AddSynaxisHealthChecks(
        this IHealthChecksBuilder builder,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Layer 1: Infrastructure Health Checks
        builder.AddCheck<InfrastructureHealthCheck>("infrastructure", tags: new[] { "layer1", "infrastructure" });

        // Layer 2: Dependency Health Checks
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            builder.AddNpgSql(connectionString, name: "postgresql", tags: new[] { "layer2", "database" });
        }

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            builder.AddRedis(redisConnectionString, name: "redis", tags: new[] { "layer2", "cache" });
        }

        var serviceBusConnectionString = configuration.GetConnectionString("ServiceBus");
        if (!string.IsNullOrEmpty(serviceBusConnectionString))
        {
            builder.AddAzureServiceBusTopicHealthCheck(
                serviceBusConnectionString,
                topicName: configuration["ServiceBus:TopicName"] ?? "synaxis-events",
                name: "servicebus",
                tags: new[] { "layer2", "messaging" });
        }

        // Layer 3: Application Health Checks
        builder.AddCheck<ApplicationHealthCheck>("application", tags: new[] { "layer3", "application" });

        // Layer 4: Business Health Checks
        builder.AddCheck<BusinessHealthCheck>("business", tags: new[] { "layer4", "business" });

        // Self check
        builder.AddCheck("self", () => HealthCheckResult.Healthy("Health check service is running"), tags: new[] { "self" });

        return builder;
    }
}
