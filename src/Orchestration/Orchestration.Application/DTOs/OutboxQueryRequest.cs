namespace Orchestration.Application.DTOs;

public record OutboxQueryRequest(
    string? Status = null,
    string? EventType = null,
    int Page = 1,
    int PageSize = 50);
