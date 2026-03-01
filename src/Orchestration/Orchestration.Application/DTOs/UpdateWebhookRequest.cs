namespace Orchestration.Application.DTOs;

public record UpdateWebhookRequest(
    string Url,
    string EventType,
    bool IsActive,
    string? Secret = null);
