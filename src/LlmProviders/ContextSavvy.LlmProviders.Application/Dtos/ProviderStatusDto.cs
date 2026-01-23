namespace ContextSavvy.LlmProviders.Application.Dtos;

public record ProviderStatusDto(string ProviderName, bool IsHealthy, string? StatusMessage, DateTime LastChecked);
