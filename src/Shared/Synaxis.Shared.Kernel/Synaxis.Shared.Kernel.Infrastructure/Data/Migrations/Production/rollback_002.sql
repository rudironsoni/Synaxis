-- Rollback: rollback_002.sql
-- Description: Rollback for 002_EventSourcingTables
-- Idempotent: Yes
-- Created: 2026-03-04

DO $$
BEGIN
    RAISE NOTICE 'Rolling back 002_EventSourcingTables...';
    
    -- Drop triggers
    DROP TRIGGER IF EXISTS trg_event_streams_updated_at ON event_streams;
    DROP TRIGGER IF EXISTS trg_event_projections_updated_at ON event_projections;
    DROP TRIGGER IF EXISTS trg_event_subscriptions_updated_at ON event_subscriptions;
    
    -- Drop functions
    DROP FUNCTION IF EXISTS append_event(VARCHAR, VARCHAR, VARCHAR, BIGINT, VARCHAR, JSONB, JSONB, UUID, UUID, UUID, VARCHAR, UUID);
    DROP FUNCTION IF EXISTS create_snapshot(VARCHAR, BIGINT, VARCHAR, JSONB, BIGINT, JSONB);
    
    -- Drop tables
    DROP TABLE IF EXISTS event_dead_letter CASCADE;
    DROP TABLE IF EXISTS event_subscriptions CASCADE;
    DROP TABLE IF EXISTS event_projections CASCADE;
    DROP TABLE IF EXISTS event_store_snapshots CASCADE;
    DROP TABLE IF EXISTS event_store CASCADE;
    DROP TABLE IF EXISTS event_streams CASCADE;
    
    DELETE FROM schema_migrations WHERE version = '002';
    RAISE NOTICE 'Rollback 002 complete';
END $$;
