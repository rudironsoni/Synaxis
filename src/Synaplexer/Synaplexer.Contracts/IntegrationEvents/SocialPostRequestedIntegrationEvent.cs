namespace Synaplexer.Contracts.IntegrationEvents;

public record SocialPostRequestedIntegrationEvent(
    Guid Id,
    DateTime CreatedAt,
    string Platform,
    string Content,
    string[] MediaUrls) : IntegrationEvent(Id, CreatedAt);
