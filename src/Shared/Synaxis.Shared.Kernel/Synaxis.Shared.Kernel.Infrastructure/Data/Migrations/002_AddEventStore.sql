-- Event Store Migration for PostgreSQL
-- Creates the event store and snapshot tables for event sourcing

-- Enable UUID extension if not already enabled
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Event Store Table
CREATE TABLE IF NOT EXISTS event_store (
    id BIGSERIAL PRIMARY KEY,
    global_position BIGSERIAL UNIQUE NOT NULL,
    stream_id VARCHAR(512) NOT NULL,
    version BIGINT NOT NULL,
    event_type VARCHAR(512) NOT NULL,
    event_data JSONB NOT NULL,
    metadata JSONB,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    event_id UUID NOT NULL,

    -- Composite unique constraint for optimistic concurrency
    CONSTRAINT uq_event_store_stream_version UNIQUE (stream_id, version)
);

-- Indexes for Event Store
CREATE INDEX IF NOT EXISTS ix_event_store_stream_id ON event_store(stream_id);
CREATE INDEX IF NOT EXISTS ix_event_store_stream_version ON event_store(stream_id, version);
CREATE INDEX IF NOT EXISTS ix_event_store_global_position ON event_store(global_position);
CREATE INDEX IF NOT EXISTS ix_event_store_event_type ON event_store(event_type);
CREATE INDEX IF NOT EXISTS ix_event_store_timestamp ON event_store(timestamp);

-- Partial index for event data queries (for JSONB operations)
CREATE INDEX IF NOT EXISTS ix_event_store_event_data ON event_store USING GIN (event_data jsonb_path_ops);

-- Snapshot Table
CREATE TABLE IF NOT EXISTS event_store_snapshots (
    id BIGSERIAL PRIMARY KEY,
    stream_id VARCHAR(512) NOT NULL UNIQUE,
    version BIGINT NOT NULL,
    aggregate_type VARCHAR(512) NOT NULL,
    state_data JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    event_count INT NOT NULL DEFAULT 0
);

-- Indexes for Snapshots
CREATE INDEX IF NOT EXISTS ix_snapshots_stream_id ON event_store_snapshots(stream_id);
CREATE INDEX IF NOT EXISTS ix_snapshots_aggregate_type ON event_store_snapshots(aggregate_type);
CREATE INDEX IF NOT EXISTS ix_snapshots_created_at ON event_store_snapshots(created_at);

-- Comment on tables
COMMENT ON TABLE event_store IS 'Stores events for event sourcing pattern. Global position provides total ordering.';
COMMENT ON TABLE event_store_snapshots IS 'Stores aggregate snapshots for performance optimization.';

-- Comment on columns
COMMENT ON COLUMN event_store.global_position IS 'Global sequence position across all streams for temporal queries';
COMMENT ON COLUMN event_store.version IS 'Stream-specific version for optimistic concurrency';
COMMENT ON COLUMN event_store.metadata IS 'Event metadata including correlation and causation IDs';
