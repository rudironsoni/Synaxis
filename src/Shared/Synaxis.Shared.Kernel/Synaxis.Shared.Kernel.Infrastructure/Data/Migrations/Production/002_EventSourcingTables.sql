-- Migration: 002_EventSourcingTables
-- Description: Event store tables for event sourcing pattern (streams, events, snapshots)
-- Idempotent: Yes
-- Created: 2026-03-04

-- ============================================================================
-- Event Streams Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS event_streams (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    stream_id VARCHAR(512) NOT NULL UNIQUE,
    aggregate_type VARCHAR(256) NOT NULL,
    aggregate_id VARCHAR(256) NOT NULL,
    current_version BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    is_archived BOOLEAN DEFAULT FALSE,
    metadata JSONB DEFAULT '{}'
);

-- Event Streams indexes
CREATE INDEX IF NOT EXISTS idx_event_streams_aggregate ON event_streams(aggregate_type, aggregate_id);
CREATE INDEX IF NOT EXISTS idx_event_streams_archived ON event_streams(is_archived) WHERE is_archived = TRUE;
CREATE UNIQUE INDEX IF NOT EXISTS idx_event_streams_unique_aggregate 
    ON event_streams(aggregate_type, aggregate_id);

-- Event Streams constraints
ALTER TABLE event_streams
    ADD CONSTRAINT IF NOT EXISTS chk_event_streams_version_non_negative CHECK (current_version >= 0);

-- ============================================================================
-- Event Store Table (main events table)
-- ============================================================================
CREATE TABLE IF NOT EXISTS event_store (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    global_position BIGSERIAL UNIQUE NOT NULL,
    stream_id VARCHAR(512) NOT NULL,
    stream_uuid UUID,
    version BIGINT NOT NULL,
    event_type VARCHAR(512) NOT NULL,
    event_data JSONB NOT NULL,
    metadata JSONB DEFAULT '{}',
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    event_id UUID DEFAULT uuid_generate_v4(),
    correlation_id UUID,
    causation_id UUID,
    actor_id UUID,
    actor_type VARCHAR(100),
    tenant_id UUID,
    is_archived BOOLEAN DEFAULT FALSE
);

-- Event Store indexes
CREATE INDEX IF NOT EXISTS idx_event_store_stream_version ON event_store(stream_id, version);
CREATE UNIQUE INDEX IF NOT EXISTS idx_event_store_unique_stream_version 
    ON event_store(stream_id, version);
CREATE INDEX IF NOT EXISTS idx_event_store_global_position ON event_store(global_position);
CREATE INDEX IF NOT EXISTS idx_event_store_event_type ON event_store(event_type);
CREATE INDEX IF NOT EXISTS idx_event_store_timestamp ON event_store(timestamp);
CREATE INDEX IF NOT EXISTS idx_event_store_correlation ON event_store(correlation_id);
CREATE INDEX IF NOT EXISTS idx_event_store_tenant ON event_store(tenant_id);
CREATE INDEX IF NOT EXISTS idx_event_store_actor ON event_store(actor_id);

-- Event Store GIN indexes for JSONB queries
CREATE INDEX IF NOT EXISTS idx_event_store_event_data_gin ON event_store USING GIN(event_data);
CREATE INDEX IF NOT EXISTS idx_event_store_metadata_gin ON event_store USING GIN(metadata);

-- Event Store constraints
ALTER TABLE event_store
    ADD CONSTRAINT IF NOT EXISTS chk_event_store_version_positive CHECK (version > 0),
    ADD CONSTRAINT IF NOT EXISTS fk_event_store_stream
        FOREIGN KEY (stream_uuid) REFERENCES event_streams(id) ON DELETE CASCADE;

-- ============================================================================
-- Event Store Snapshots Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS event_store_snapshots (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    stream_id VARCHAR(512) NOT NULL,
    stream_uuid UUID,
    version BIGINT NOT NULL,
    aggregate_type VARCHAR(512) NOT NULL,
    state_data JSONB NOT NULL,
    event_count BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    metadata JSONB DEFAULT '{}',
    is_active BOOLEAN DEFAULT TRUE
);

-- Snapshots indexes
CREATE INDEX IF NOT EXISTS idx_snapshots_stream_id ON event_store_snapshots(stream_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_snapshots_unique_stream 
    ON event_store_snapshots(stream_id) WHERE is_active = TRUE;
CREATE INDEX IF NOT EXISTS idx_snapshots_aggregate_type ON event_store_snapshots(aggregate_type);
CREATE INDEX IF NOT EXISTS idx_snapshots_version ON event_store_snapshots(version);
CREATE INDEX IF NOT EXISTS idx_snapshots_created_at ON event_store_snapshots(created_at);

-- Snapshots GIN indexes
CREATE INDEX IF NOT EXISTS idx_snapshots_state_data_gin ON event_store_snapshots USING GIN(state_data);

-- Snapshots constraints
ALTER TABLE event_store_snapshots
    ADD CONSTRAINT IF NOT EXISTS chk_snapshots_version_positive CHECK (version > 0),
    ADD CONSTRAINT IF NOT EXISTS chk_snapshots_event_count_non_negative CHECK (event_count >= 0),
    ADD CONSTRAINT IF NOT EXISTS fk_snapshots_stream
        FOREIGN KEY (stream_uuid) REFERENCES event_streams(id) ON DELETE CASCADE;

-- ============================================================================
-- Projections Table (for read model projections)
-- ============================================================================
CREATE TABLE IF NOT EXISTS event_projections (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    projection_name VARCHAR(256) NOT NULL,
    projection_type VARCHAR(100) NOT NULL,
    aggregate_type VARCHAR(256) NOT NULL,
    aggregate_id VARCHAR(256) NOT NULL,
    last_event_position BIGINT NOT NULL DEFAULT 0,
    last_event_timestamp TIMESTAMP WITH TIME ZONE,
    data JSONB NOT NULL DEFAULT '{}',
    metadata JSONB DEFAULT '{}',
    checksum VARCHAR(64),
    version INTEGER DEFAULT 1,
    is_stale BOOLEAN DEFAULT FALSE,
    error_count INTEGER DEFAULT 0,
    last_error_message TEXT,
    last_error_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Projections indexes
CREATE UNIQUE INDEX IF NOT EXISTS idx_projections_unique 
    ON event_projections(projection_name, aggregate_type, aggregate_id);
CREATE INDEX IF NOT EXISTS idx_projections_name ON event_projections(projection_name);
CREATE INDEX IF NOT EXISTS idx_projections_aggregate ON event_projections(aggregate_type, aggregate_id);
CREATE INDEX IF NOT EXISTS idx_projections_stale ON event_projections(is_stale) WHERE is_stale = TRUE;
CREATE INDEX IF NOT EXISTS idx_projections_updated_at ON event_projections(updated_at);

-- Projections GIN indexes
CREATE INDEX IF NOT EXISTS idx_projections_data_gin ON event_projections USING GIN(data);

-- Projections constraints
ALTER TABLE event_projections
    ADD CONSTRAINT IF NOT EXISTS chk_projections_position_non_negative CHECK (last_event_position >= 0),
    ADD CONSTRAINT IF NOT EXISTS chk_projections_error_count_non_negative CHECK (error_count >= 0);

-- ============================================================================
-- Event Subscriptions Table (for subscription management)
-- ============================================================================
CREATE TABLE IF NOT EXISTS event_subscriptions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    subscription_name VARCHAR(256) NOT NULL UNIQUE,
    subscription_type VARCHAR(100) NOT NULL DEFAULT 'catch_up',
    event_types TEXT[] DEFAULT '{}',
    aggregate_types TEXT[] DEFAULT '{}',
    last_processed_position BIGINT NOT NULL DEFAULT 0,
    last_processed_timestamp TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT TRUE,
    failure_count INTEGER DEFAULT 0,
    last_failure_at TIMESTAMP WITH TIME ZONE,
    last_failure_message TEXT,
    checkpoint_interval INTEGER DEFAULT 100,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Subscriptions indexes
CREATE INDEX IF NOT EXISTS idx_subscriptions_active ON event_subscriptions(is_active);
CREATE INDEX IF NOT EXISTS idx_subscriptions_position ON event_subscriptions(last_processed_position);

-- Subscriptions constraints
ALTER TABLE event_subscriptions
    ADD CONSTRAINT IF NOT EXISTS chk_subscriptions_position_non_negative CHECK (last_processed_position >= 0),
    ADD CONSTRAINT IF NOT EXISTS chk_subscriptions_failure_count_non_negative CHECK (failure_count >= 0),
    ADD CONSTRAINT IF NOT EXISTS chk_subscriptions_checkpoint_interval_positive CHECK (checkpoint_interval > 0);

-- ============================================================================
-- Dead Letter Events Table (for failed event processing)
-- ============================================================================
CREATE TABLE IF NOT EXISTS event_dead_letter (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_id UUID NOT NULL,
    stream_id VARCHAR(512) NOT NULL,
    event_type VARCHAR(512) NOT NULL,
    event_data JSONB NOT NULL,
    metadata JSONB DEFAULT '{}',
    failure_reason TEXT NOT NULL,
    failure_category VARCHAR(100) NOT NULL,
    subscription_name VARCHAR(256),
    retry_count INTEGER DEFAULT 0,
    max_retries INTEGER DEFAULT 3,
    next_retry_at TIMESTAMP WITH TIME ZONE,
    resolved_at TIMESTAMP WITH TIME ZONE,
    resolution_notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Dead Letter indexes
CREATE INDEX IF NOT EXISTS idx_dead_letter_event_id ON event_dead_letter(event_id);
CREATE INDEX IF NOT EXISTS idx_dead_letter_stream ON event_dead_letter(stream_id);
CREATE INDEX IF NOT EXISTS idx_dead_letter_retry ON event_dead_letter(next_retry_at) WHERE resolved_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_dead_letter_subscription ON event_dead_letter(subscription_name);

-- Dead Letter constraints
ALTER TABLE event_dead_letter
    ADD CONSTRAINT IF NOT EXISTS chk_dead_letter_retry_count_non_negative CHECK (retry_count >= 0),
    ADD CONSTRAINT IF NOT EXISTS chk_dead_letter_max_retries_positive CHECK (max_retries > 0);

-- ============================================================================
-- Functions for Event Sourcing
-- ============================================================================

-- Function to append an event and update stream version atomically
CREATE OR REPLACE FUNCTION append_event(
    p_stream_id VARCHAR(512),
    p_aggregate_type VARCHAR(256),
    p_aggregate_id VARCHAR(256),
    p_expected_version BIGINT,
    p_event_type VARCHAR(512),
    p_event_data JSONB,
    p_metadata JSONB DEFAULT '{}',
    p_correlation_id UUID DEFAULT NULL,
    p_causation_id UUID DEFAULT NULL,
    p_actor_id UUID DEFAULT NULL,
    p_actor_type VARCHAR(100) DEFAULT NULL,
    p_tenant_id UUID DEFAULT NULL
) RETURNS TABLE(event_version BIGINT, global_pos BIGINT) AS $$
DECLARE
    v_stream_uuid UUID;
    v_current_version BIGINT;
    v_new_version BIGINT;
    v_global_pos BIGINT;
BEGIN
    -- Get or create stream
    SELECT es.id, es.current_version INTO v_stream_uuid, v_current_version
    FROM event_streams es
    WHERE es.stream_id = p_stream_id
    FOR UPDATE;

    IF v_stream_uuid IS NULL THEN
        -- Create new stream
        INSERT INTO event_streams (stream_id, aggregate_type, aggregate_id, current_version)
        VALUES (p_stream_id, p_aggregate_type, p_aggregate_id, 0)
        RETURNING id INTO v_stream_uuid;
        v_current_version := 0;
    END IF;

    -- Check expected version for optimistic concurrency
    IF p_expected_version IS NOT NULL AND v_current_version != p_expected_version THEN
        RAISE EXCEPTION 'Concurrency conflict: expected version %, but current version is %',
            p_expected_version, v_current_version;
    END IF;

    -- Calculate new version
    v_new_version := v_current_version + 1;

    -- Insert event
    INSERT INTO event_store (
        stream_id, stream_uuid, version, event_type, event_data, metadata,
        correlation_id, causation_id, actor_id, actor_type, tenant_id
    ) VALUES (
        p_stream_id, v_stream_uuid, v_new_version, p_event_type, p_event_data, p_metadata,
        p_correlation_id, p_causation_id, p_actor_id, p_actor_type, p_tenant_id
    )
    RETURNING event_store.version, event_store.global_position 
    INTO v_new_version, v_global_pos;

    -- Update stream version
    UPDATE event_streams
    SET current_version = v_new_version, updated_at = NOW()
    WHERE id = v_stream_uuid;

    RETURN QUERY SELECT v_new_version, v_global_pos;
END;
$$ LANGUAGE plpgsql;

-- Function to create a snapshot
CREATE OR REPLACE FUNCTION create_snapshot(
    p_stream_id VARCHAR(512),
    p_version BIGINT,
    p_aggregate_type VARCHAR(512),
    p_state_data JSONB,
    p_event_count BIGINT DEFAULT 0,
    p_metadata JSONB DEFAULT '{}'
) RETURNS UUID AS $$
DECLARE
    v_stream_uuid UUID;
    v_snapshot_id UUID;
BEGIN
    -- Get stream UUID
    SELECT id INTO v_stream_uuid
    FROM event_streams
    WHERE stream_id = p_stream_id;

    IF v_stream_uuid IS NULL THEN
        RAISE EXCEPTION 'Stream not found: %', p_stream_id;
    END IF;

    -- Mark existing snapshots as inactive
    UPDATE event_store_snapshots
    SET is_active = FALSE
    WHERE stream_id = p_stream_id AND is_active = TRUE;

    -- Create new snapshot
    INSERT INTO event_store_snapshots (
        stream_id, stream_uuid, version, aggregate_type, state_data, event_count, metadata
    ) VALUES (
        p_stream_id, v_stream_uuid, p_version, p_aggregate_type, p_state_data, p_event_count, p_metadata
    )
    RETURNING id INTO v_snapshot_id;

    RETURN v_snapshot_id;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- Triggers
-- ============================================================================
DO $$
BEGIN
    -- Event streams updated_at trigger
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_event_streams_updated_at') THEN
        CREATE TRIGGER trg_event_streams_updated_at
        BEFORE UPDATE ON event_streams
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    -- Projections updated_at trigger
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_event_projections_updated_at') THEN
        CREATE TRIGGER trg_event_projections_updated_at
        BEFORE UPDATE ON event_projections
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    -- Subscriptions updated_at trigger
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_event_subscriptions_updated_at') THEN
        CREATE TRIGGER trg_event_subscriptions_updated_at
        BEFORE UPDATE ON event_subscriptions
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;
END $$;

-- ============================================================================
-- Migration Completion Log
-- ============================================================================
INSERT INTO schema_migrations (version, applied_at, description)
VALUES ('002', NOW(), 'EventSourcingTables: event_streams, event_store, event_store_snapshots, event_projections, event_subscriptions, event_dead_letter')
ON CONFLICT (version) DO UPDATE SET
    applied_at = EXCLUDED.applied_at,
    description = EXCLUDED.description;
