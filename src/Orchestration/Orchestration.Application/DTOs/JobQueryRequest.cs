namespace Orchestration.Application.DTOs;

public record JobQueryRequest(
    string? Status = null,
    string? JobType = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 50);
