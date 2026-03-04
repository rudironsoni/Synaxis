-- =============================================================================
-- Migration: 002_EventSourcingSchema
-- Description: Event sourcing tables for aggregate persistence
-- Created: 2026-03-04
-- Idempotent: Yes
-- Dependencies: 001_InitialSchema
-- =============================================================================

-- =============================================================================
-- EVENT STORE TABLES
-- =============================================================================

-- Event Store table
CREATE TABLE IF NOT EXISTS event_store (
    id BIGSERIAL PRIMARY KEY,
    global_position BIGSERIAL NOT NULL UNIQUE,
    stream_id VARCHAR(512) NOT NULL,
    version BIGINT NOT NULL,
    event_type VARCHAR(512) NOT NULL,
    event_data JSONB NOT NULL,
    metadata JSONB,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    event_id UUID NOT NULL UNIQUE,

    CONSTRAINT uq_event_store_stream_version UNIQUE (stream_id, version)
);

CREATE INDEX IF NOT EXISTS idx_event_store_stream_version ON event_store(stream_id, version);
CREATE INDEX IF NOT EXISTS idx_event_store_global_position ON event_store(global_position);
CREATE INDEX IF NOT EXISTS idx_event_store_event_type ON event_store(event_type);
CREATE INDEX IF NOT EXISTS idx_event_store_timestamp ON event_store(timestamp);

-- Event Store Snapshots table
CREATE TABLE IF NOT EXISTS event_store_snapshots (
    id BIGSERIAL PRIMARY KEY,
    stream_id VARCHAR(512) NOT NULL UNIQUE,
    version BIGINT NOT NULL,
    aggregate_type VARCHAR(512) NOT NULL,
    state_data JSONB NOT NULL,
    event_count BIGINT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_snapshots_stream_id ON event_store_snapshots(stream_id);
CREATE INDEX IF NOT EXISTS idx_snapshots_aggregate_type ON event_store_snapshots(aggregate_type);

-- =============================================================================
-- EVENT STORE PROJECTIONS
-- =============================================================================

-- Aggregate Status view for quick lookups
CREATE OR REPLACE VIEW event_store_aggregate_status AS
SELECT
    stream_id,
    aggregate_type,
    MAX(version) as current_version,
    COUNT(*) as event_count,
    MAX(timestamp) as last_event_at
FROM event_store
GROUP BY stream_id, aggregate_type;

-- =============================================================================
-- EVENT STORE FUNCTIONS
-- =============================================================================

-- Function to get events for a stream
CREATE OR REPLACE FUNCTION get_stream_events(
    p_stream_id VARCHAR(512),
    p_from_version BIGINT DEFAULT 0,
    p_limit INTEGER DEFAULT 1000
)
RETURNS TABLE (
    id BIGINT,
    global_position BIGINT,
    stream_id VARCHAR(512),
    version BIGINT,
    event_type VARCHAR(512),
    event_data JSONB,
    metadata JSONB,
    timestamp TIMESTAMPTZ,
    event_id UUID
) AS $$
BEGIN
    RETURN QUERY
    SELECT e.id, e.global_position, e.stream_id, e.version, e.event_type,
           e.event_data, e.metadata, e.timestamp, e.event_id
    FROM event_store e
    WHERE e.stream_id = p_stream_id
      AND e.version > p_from_version
    ORDER BY e.version ASC
    LIMIT p_limit;
END;
$$ LANGUAGE plpgsql STABLE;

-- Function to append event with optimistic concurrency
CREATE OR REPLACE FUNCTION append_event(
    p_stream_id VARCHAR(512),
    p_expected_version BIGINT,
    p_event_type VARCHAR(512),
    p_event_data JSONB,
    p_metadata JSONB DEFAULT NULL,
    p_event_id UUID DEFAULT NULL
)
RETURNS TABLE (
    new_version BIGINT,
    global_position BIGINT
) AS $$
DECLARE
    v_current_version BIGINT;
    v_new_version BIGINT;
    v_global_position BIGINT;
BEGIN
    -- Get current version with lock
    SELECT COALESCE(MAX(version), 0)
    INTO v_current_version
    FROM event_store
    WHERE stream_id = p_stream_id
    FOR UPDATE;

    -- Check optimistic concurrency
    IF v_current_version != p_expected_version THEN
        RAISE EXCEPTION 'Concurrency conflict: expected version %, but found %',
            p_expected_version, v_current_version
            USING ERRCODE = 'P0001';
    END IF;

    -- Calculate new version
    v_new_version := v_current_version + 1;

    -- Insert event
    INSERT INTO event_store (
        stream_id, version, event_type, event_data, metadata, event_id
    ) VALUES (
        p_stream_id, v_new_version, p_event_type, p_event_data, p_metadata,
        COALESCE(p_event_id, uuid_generate_v4())
    )
    RETURNING event_store.global_position INTO v_global_position;

    RETURN QUERY SELECT v_new_version, v_global_position;
END;
$$ LANGUAGE plpgsql;

-- Function to save snapshot
CREATE OR REPLACE FUNCTION save_snapshot(
    p_stream_id VARCHAR(512),
    p_version BIGINT,
    p_aggregate_type VARCHAR(512),
    p_state_data JSONB,
    p_event_count BIGINT
)
RETURNS VOID AS $$
BEGIN
    INSERT INTO event_store_snapshots (
        stream_id, version, aggregate_type, state_data, event_count
    ) VALUES (
        p_stream_id, p_version, p_aggregate_type, p_state_data, p_event_count
    )
    ON CONFLICT (stream_id) DO UPDATE SET
        version = EXCLUDED.version,
        state_data = EXCLUDED.state_data,
        event_count = EXCLUDED.event_count,
        created_at = NOW();
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- EVENT STORE CLEANUP PROCEDURES
-- =============================================================================

-- Function to archive old events
CREATE OR REPLACE FUNCTION archive_events_before(
    p_timestamp TIMESTAMPTZ,
    p_archive_table_name TEXT DEFAULT NULL
)
RETURNS INTEGER AS $$
DECLARE
    v_archive_table TEXT;
    v_count INTEGER;
BEGIN
    -- Generate archive table name if not provided
    v_archive_table := COALESCE(
        p_archive_table_name,
        'event_store_archive_' || TO_CHAR(NOW(), 'YYYY_MM')
    );

    -- Create archive table if it doesn't exist
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I (
            LIKE event_store INCLUDING ALL
        )', v_archive_table);

    -- Move events to archive
    EXECUTE format('
        WITH moved_events AS (
            DELETE FROM event_store
            WHERE timestamp < $1
            RETURNING *
        )
        INSERT INTO %I SELECT * FROM moved_events', v_archive_table)
    USING p_timestamp;

    GET DIAGNOSTICS v_count = ROW_COUNT;

    RETURN v_count;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- MIGRATION METADATA
-- =============================================================================

-- Record this migration
INSERT INTO __migrations_history (migration_id, migration_name, checksum, is_idempotent)
VALUES ('002', 'EventSourcingSchema', MD5(pg_read_file('/scripts/migrations/ef-core/002_EventSourcingSchema.sql')::text), true)
ON CONFLICT (migration_id) DO NOTHING;

-- =============================================================================
-- COMMENTS
-- =============================================================================

COMMENT ON TABLE event_store IS 'Event store for event sourcing pattern';
COMMENT ON TABLE event_store_snapshots IS 'Aggregate snapshots for performance';
COMMENT ON FUNCTION append_event IS 'Appends event with optimistic concurrency control';
COMMENT ON FUNCTION get_stream_events IS 'Retrieves events for a stream from a specific version';
COMMENT ON FUNCTION save_snapshot IS 'Saves or updates an aggregate snapshot';
