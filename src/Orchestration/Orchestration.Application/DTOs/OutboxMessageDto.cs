namespace Orchestration.Application.DTOs;

public record OutboxMessageDto(
    Guid Id,
    string EventType,
    string Payload,
    string Status,
    int RetryCount,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    string? ErrorMessage);
