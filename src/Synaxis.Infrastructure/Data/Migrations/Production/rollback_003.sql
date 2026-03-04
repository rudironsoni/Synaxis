-- Rollback: rollback_003.sql
-- Description: Rollback for 003_ApiManagementTables
-- Idempotent: Yes
-- Created: 2026-03-04

DO $$
BEGIN
    RAISE NOTICE 'Rolling back 003_ApiManagementTables...';
    
    -- Drop triggers
    DROP TRIGGER IF EXISTS trg_api_keys_updated_at ON api_keys;
    DROP TRIGGER IF EXISTS trg_rate_limit_configs_updated_at ON rate_limit_configs;
    DROP TRIGGER IF EXISTS trg_rate_limit_assignments_updated_at ON rate_limit_assignments;
    DROP TRIGGER IF EXISTS trg_rate_limit_tracking_updated_at ON rate_limit_tracking;
    DROP TRIGGER IF EXISTS trg_api_endpoints_updated_at ON api_endpoints;
    
    -- Drop tables (order matters for FKs)
    DROP TABLE IF EXISTS api_key_usage_logs CASCADE;
    DROP TABLE IF EXISTS api_endpoints CASCADE;
    DROP TABLE IF EXISTS rate_limit_tracking CASCADE;
    DROP TABLE IF EXISTS rate_limit_assignments CASCADE;
    DROP TABLE IF EXISTS rate_limit_configs CASCADE;
    DROP TABLE IF EXISTS api_keys CASCADE;
    
    DELETE FROM schema_migrations WHERE version = '003';
    RAISE NOTICE 'Rollback 003 complete';
END $$;
