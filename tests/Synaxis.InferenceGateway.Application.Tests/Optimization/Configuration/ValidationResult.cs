namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Configuration;

public class ValidationResult
{
    public bool IsValid { get; set; }

    public List<string> Errors { get; set; } = new();
}
