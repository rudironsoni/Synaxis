namespace Synaxis.InferenceGateway.Application.RealTime;

/// <summary>
/// Real-time notification when cost optimization is applied.
/// </summary>
public record CostOptimizationResult(
    Guid OrganizationId,
    string FromProvider,
    string ToProvider,
    string Reason,
    decimal SavingsPer1MTokens,
    DateTime AppliedAt
);
