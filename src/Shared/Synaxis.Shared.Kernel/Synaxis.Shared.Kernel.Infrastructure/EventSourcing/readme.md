# Event Sourcing Infrastructure

This folder contains the event sourcing infrastructure for Synaxis, enabling audit trails, event replay, and temporal queries.

## Core Components

### Interfaces
- **IEventStore** - Main interface for appending and reading events
- **IEventEnvelope** - Envelope for event metadata and data
- **IEventSerializer** - JSON serialization with polymorphic support
- **ISnapshotStore** - Snapshot support for performance

### PostgreSQL Implementation
- **PostgreSqlEventStore** - Full event store implementation with optimistic concurrency
- **PostgreSqlSnapshotStore** - Snapshot storage for aggregate performance
- **EventStoreDbContext** - EF Core configuration

### Aggregates
- **EventSourcedAggregate** - Base class for event-sourced aggregates
- **IAggregateSnapshot** - Interface for aggregates that support snapshots

### Integration
- **EventStoreRepository<T>** - Repository for loading and saving aggregates
- **DomainEventPublisher** - Publishes events via MediatR
- **EventStoreServiceExtensions** - DI registration helpers

## Database Schema

The event store uses two tables:

**event_store**:
- `id` - Primary key
- `global_position` - Global ordering across all streams
- `stream_id` - Aggregate/stream identifier
- `version` - Stream-specific version (optimistic concurrency)
- `event_type` - CLR type name for polymorphic deserialization
- `event_data` - JSONB event payload
- `metadata` - JSONB correlation, causation, user info
- `timestamp` - Event timestamp
- `event_id` - Unique event GUID

**event_store_snapshots**:
- `stream_id` - Aggregate identifier
- `version` - Version at snapshot time
- `aggregate_type` - CLR type name
- `state_data` - JSONB serialized state
- `created_at` - Snapshot timestamp

## Usage

```csharp
// Register services
services.AddEventSourcing(connectionString)
        .AddEventSourcedAggregate<OrderAggregate>();

// Repository usage
public class OrderService
{
    private readonly EventStoreRepository<OrderAggregate> _repository;
    
    public async Task<OrderAggregate> GetOrder(string orderId)
    {
        return await _repository.GetByIdAsync(orderId);
    }
    
    public async Task SaveOrder(OrderAggregate order)
    {
        await _repository.SaveAsync(order);
    }
}
```

## Features
- Optimistic concurrency with version checking
- Global event ordering for temporal queries
- Polymorphic event serialization with System.Text.Json
- Snapshot support for large aggregates
- Correlation and causation tracking
- Full integration with MediatR notifications
