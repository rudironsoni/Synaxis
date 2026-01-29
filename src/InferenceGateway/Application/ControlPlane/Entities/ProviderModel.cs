using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class ProviderModel
{
    [Key]
    public int Id { get; set; }

    public string ProviderId { get; set; } = string.Empty; // e.g. "nvidia"

    // FK to GlobalModel
    public string GlobalModelId { get; set; } = string.Empty;
    public string ProviderSpecificId { get; set; } = string.Empty; // ID sent to the provider API

    public bool IsAvailable { get; set; }

    public decimal? OverrideInputPrice { get; set; }
    public decimal? OverrideOutputPrice { get; set; }

    public int? RateLimitRPM { get; set; }
    public int? RateLimitTPM { get; set; }

    // Navigation
    [ForeignKey("GlobalModelId")]
    public GlobalModel? GlobalModel { get; set; }
}
