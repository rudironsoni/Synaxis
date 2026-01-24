namespace Synaplexer.Contracts.IntegrationEvents;

public record IntegrationEvent(Guid Id, DateTime CreatedAt)
{
    public IntegrationEvent() : this(Guid.NewGuid(), DateTime.UtcNow) { }
}
