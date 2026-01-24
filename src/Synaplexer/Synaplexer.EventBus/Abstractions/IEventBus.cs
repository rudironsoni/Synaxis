using Synaplexer.Contracts.IntegrationEvents;

namespace Synaplexer.EventBus.Abstractions;

public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) where T : IntegrationEvent;
    
    void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>;

    void Unsubscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>;
}
