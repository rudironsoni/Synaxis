using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class TenantModelLimit
{
    [Key]
    public int Id { get; set; }

    public string TenantId { get; set; } = string.Empty;

    // FK to GlobalModel
    public string GlobalModelId { get; set; } = string.Empty;

    public int? AllowedRPM { get; set; }
    public decimal? MonthlyBudget { get; set; }

    [ForeignKey("GlobalModelId")]
    public GlobalModel? GlobalModel { get; set; }
}
