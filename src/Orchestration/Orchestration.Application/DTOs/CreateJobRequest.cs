namespace Orchestration.Application.DTOs;

public record CreateJobRequest(
    string JobType,
    string Payload,
    DateTime? ScheduledAt = null);
