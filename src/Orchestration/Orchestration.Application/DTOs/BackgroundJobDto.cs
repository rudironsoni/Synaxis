namespace Orchestration.Application.DTOs;

public record BackgroundJobDto(
    Guid Id,
    string JobType,
    string Status,
    string? Payload,
    DateTime CreatedAt,
    DateTime? ScheduledAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage,
    int RetryCount);
