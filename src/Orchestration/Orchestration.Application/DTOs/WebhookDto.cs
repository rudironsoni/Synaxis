namespace Orchestration.Application.DTOs;

public record WebhookDto(
    Guid Id,
    string Url,
    string EventType,
    bool IsActive,
    int RetryCount,
    DateTime CreatedAt,
    DateTime? LastDeliveredAt);
