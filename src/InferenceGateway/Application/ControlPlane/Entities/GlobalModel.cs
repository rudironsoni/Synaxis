using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class GlobalModel
{
    [Key]
    public string Id { get; set; } = string.Empty; // canonical id e.g. "llama-3.3-70b"

    public string Name { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ReleaseDate { get; set; }

    public int ContextWindow { get; set; } = 0;
    public int MaxOutputTokens { get; set; } = 0;

    public decimal InputPrice { get; set; }
    public decimal OutputPrice { get; set; }

    public bool IsOpenWeights { get; set; }

    // Capabilities
    public bool SupportsTools { get; set; }
    public bool SupportsReasoning { get; set; }
    public bool SupportsVision { get; set; }
    public bool SupportsAudio { get; set; }
    public bool SupportsStructuredOutput { get; set; }

    // Navigation
    public List<ProviderModel> ProviderModels { get; set; } = new();
}
