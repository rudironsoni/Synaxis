namespace Orchestration.Application.DTOs;

public record JobStatusDto(
    Guid Id,
    string Status,
    double ProgressPercent,
    string? CurrentStep,
    DateTime? EstimatedCompletion);
