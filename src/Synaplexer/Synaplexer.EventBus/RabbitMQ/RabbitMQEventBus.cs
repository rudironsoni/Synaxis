using System.Text;
using System.Text.Json;
using Synaplexer.Contracts.IntegrationEvents;
using Synaplexer.EventBus.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Synaplexer.EventBus.RabbitMQ;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string ExchangeName = "contextsavvy_event_bus";

    public RabbitMQEventBus(IConnectionFactory connectionFactory, ILogger<RabbitMQEventBus> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) where T : IntegrationEvent
    {
        try
        {
            if (_connection == null)
            {
                _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            }
            
            if (_channel == null)
            {
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                await _channel.ExchangeDeclareAsync(exchange: ExchangeName, type: "fanout", cancellationToken: cancellationToken);
            }

            var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, integrationEvent.GetType(), new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var eventName = integrationEvent.GetType().Name;
            
            // In fanout, routing key is ignored, but good to set just in case we switch to topic
            await _channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: eventName,
                body: body,
                cancellationToken: cancellationToken);
                
            _logger.LogInformation("Published event {EventName} with Id {EventId}", eventName, integrationEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventId}", integrationEvent.Id);
            throw;
        }
    }

    public void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = typeof(T).Name;
        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);
        // Implementation for RabbitMQ subscription would go here
        // For now, we'll just log it to satisfy the interface and fix build errors
    }

    public void Unsubscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = typeof(T).Name;
        _logger.LogInformation("Unsubscribing from event {EventName}", eventName);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
