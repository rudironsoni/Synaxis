namespace Orchestration.Application.DTOs;

public record CreateWebhookRequest(
    string Url,
    string EventType,
    string? Secret = null);
