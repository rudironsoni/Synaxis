namespace Orchestration.Application.DTOs;

public record WebhookTestResultDto(
    bool Success,
    int StatusCode,
    string? ResponseBody,
    long ResponseTimeMs);
