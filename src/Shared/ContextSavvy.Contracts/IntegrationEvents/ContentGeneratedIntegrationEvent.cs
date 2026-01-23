namespace ContextSavvy.Contracts.IntegrationEvents;

public record ContentGeneratedIntegrationEvent(
    Guid Id,
    DateTime CreatedAt,
    string ContentType,
    string Content,
    string[] Tags) : IntegrationEvent(Id, CreatedAt);
