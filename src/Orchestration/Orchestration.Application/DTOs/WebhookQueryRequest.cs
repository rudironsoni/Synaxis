namespace Orchestration.Application.DTOs;

public record WebhookQueryRequest(
    string? EventType = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 50);
