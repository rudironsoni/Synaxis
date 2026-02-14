// =============================================================================
// Stamp Custom Resource Definition (CRD)
// Kubernetes Custom Resource for Ephemeral Scale Units
// =============================================================================

using System.Text.Json.Serialization;
using k8s;
using k8s.Models;

namespace Synaxis.StampController.CRDs;

/// <summary>
/// Stamp Custom Resource Definition
/// Represents an ephemeral scale unit in the Synaxis architecture
/// </summary>
public class StampResource : CustomResource<StampSpec, StampStatus>
{
    public override string ToString() => $"Stamp {Metadata.Name} (Region: {Spec.Region}, Status: {Status?.Phase ?? "Unknown"})";
}

/// <summary>
/// Stamp specification
/// </summary>
public class StampSpec
{
    [JsonPropertyName("region")]
    public required string Region { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }

    [JsonPropertyName("capacity")]
    public StampCapacity Capacity { get; set; } = new();

    [JsonPropertyName("autoScaling")]
    public AutoScalingConfig AutoScaling { get; set; } = new();

    [JsonPropertyName("networking")]
    public NetworkingConfig Networking { get; set; } = new();

    [JsonPropertyName("ttl")]
    public TimeToLiveConfig TTL { get; set; } = new();

    [JsonPropertyName("drainTimeoutMinutes")]
    public int DrainTimeoutMinutes { get; set; } = 30;

    [JsonPropertyName("quarantineTimeoutMinutes")]
    public int QuarantineTimeoutMinutes { get; set; } = 10;
}

/// <summary>
/// Stamp capacity configuration
/// </summary>
public class StampCapacity
{
    [JsonPropertyName("maxConcurrentSessions")]
    public int MaxConcurrentSessions { get; set; } = 1000;

    [JsonPropertyName("maxRps")]
    public int MaxRps { get; set; } = 500;

    [JsonPropertyName("maxMemoryGb")]
    public int MaxMemoryGb { get; set; } = 32;

    [JsonPropertyName("maxCpuCores")]
    public int MaxCpuCores { get; set; } = 8;
}

/// <summary>
/// Auto-scaling configuration
/// </summary>
public class AutoScalingConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("minNodes")]
    public int MinNodes { get; set; } = 3;

    [JsonPropertyName("maxNodes")]
    public int MaxNodes { get; set; } = 20;

    [JsonPropertyName("targetCpuUtilization")]
    public int TargetCpuUtilization { get; set; } = 70;

    [JsonPropertyName("targetMemoryUtilization")]
    public int TargetMemoryUtilization { get; set; } = 80;
}

/// <summary>
/// Networking configuration
/// </summary>
public class NetworkingConfig
{
    [JsonPropertyName("vnetCidr")]
    public required string VnetCidr { get; set; }

    [JsonPropertyName("subnetCidr")]
    public required string SubnetCidr { get; set; }

    [JsonPropertyName("loadBalancerIp")]
    public string? LoadBalancerIp { get; set; }

    [JsonPropertyName("privateEndpointEnabled")]
    public bool PrivateEndpointEnabled { get; set; } = true;
}

/// <summary>
/// Time-to-live configuration
/// </summary>
public class TimeToLiveConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("durationHours")]
    public int DurationHours { get; set; } = 24;
}

/// <summary>
/// Stamp status
/// </summary>
public class StampStatus
{
    [JsonPropertyName("phase")]
    public StampPhase Phase { get; set; } = StampPhase.Pending;

    [JsonPropertyName("conditions")]
    public List<StampCondition> Conditions { get; set; } = new();

    [JsonPropertyName("stampId")]
    public string? StampId { get; set; }

    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    [JsonPropertyName("health")]
    public StampHealth Health { get; set; } = new();

    [JsonPropertyName("metrics")]
    public StampMetrics Metrics { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("provisionedAt")]
    public DateTime? ProvisionedAt { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName("lastHealthCheck")]
    public DateTime? LastHealthCheck { get; set; }

    [JsonPropertyName("observedGeneration")]
    public long ObservedGeneration { get; set; }
}

/// <summary>
/// Stamp lifecycle phases
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StampPhase
{
    Pending,
    Provisioning,
    Ready,
    Scaling,
    Degraded,
    Draining,
    Quarantine,
    Decommissioning,
    Archived,
    Terminating,
    Failed
}

/// <summary>
/// Stamp condition
/// </summary>
public class StampCondition
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("status")]
    public ConditionStatus Status { get; set; }

    [JsonPropertyName("lastTransitionTime")]
    public DateTime? LastTransitionTime { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConditionStatus
{
    True,
    False,
    Unknown
}

/// <summary>
/// Stamp health information
/// </summary>
public class StampHealth
{
    [JsonPropertyName("overall")]
    public HealthStatus Overall { get; set; } = HealthStatus.Unknown;

    [JsonPropertyName("kubernetes")]
    public HealthStatus Kubernetes { get; set; } = HealthStatus.Unknown;

    [JsonPropertyName("networking")]
    public HealthStatus Networking { get; set; } = HealthStatus.Unknown;

    [JsonPropertyName("storage")]
    public HealthStatus Storage { get; set; } = HealthStatus.Unknown;

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

/// <summary>
/// Stamp metrics
/// </summary>
public class StampMetrics
{
    [JsonPropertyName("cpuUtilization")]
    public double CpuUtilization { get; set; }

    [JsonPropertyName("memoryUtilization")]
    public double MemoryUtilization { get; set; }

    [JsonPropertyName("activeConnections")]
    public int ActiveConnections { get; set; }

    [JsonPropertyName("requestRate")]
    public double RequestRate { get; set; }

    [JsonPropertyName("errorRate")]
    public double ErrorRate { get; set; }

    [JsonPropertyName("latencyP99")]
    public double LatencyP99 { get; set; }
}

/// <summary>
/// Stamp CRD definition
/// </summary>
public static class StampCrdDefinition
{
    public const string Group = "synaxis.io";
    public const string Version = "v1";
    public const string Plural = "stamps";
    public const string Singular = "stamp";
    public const string Kind = "Stamp";
    public const string ShortName = "st";

    public static CustomResourceDefinition CreateCrd()
    {
        return new CustomResourceDefinition()
        {
            ApiVersion = "apiextensions.k8s.io/v1",
            Kind = "CustomResourceDefinition",
            Metadata = new V1ObjectMeta
            {
                Name = $"{Plural}.{Group}"
            },
            Spec = new V1CustomResourceDefinitionSpec
            {
                Group = Group,
                Names = new V1CustomResourceDefinitionNames
                {
                    Plural = Plural,
                    Singular = Singular,
                    Kind = Kind,
                    ShortNames = new List<string> { ShortName }
                },
                Scope = "Namespaced",
                Versions = new List<V1CustomResourceDefinitionVersion>
                {
                    new()
                    {
                        Name = Version,
                        Served = true,
                        Storage = true,
                        Schema = new V1CustomResourceValidation
                        {
                            OpenAPIV3Schema = new V1JSONSchemaProps
                            {
                                Type = "object",
                                Properties = new Dictionary<string, V1JSONSchemaProps>
                                {
                                    ["spec"] = new()
                                    {
                                        Type = "object",
                                        Required = new List<string> { "region", "version", "networking" },
                                        Properties = new Dictionary<string, V1JSONSchemaProps>
                                        {
                                            ["region"] = new() { Type = "string" },
                                            ["version"] = new() { Type = "string" },
                                            ["drainTimeoutMinutes"] = new() { Type = "integer", Minimum = 1 },
                                            ["quarantineTimeoutMinutes"] = new() { Type = "integer", Minimum = 1 }
                                        }
                                    },
                                    ["status"] = new()
                                    {
                                        Type = "object"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}
