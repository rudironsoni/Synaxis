-- Migration: 000_CreateSchemaMigrationsTable
-- Description: Create the schema_migrations tracking table
-- Idempotent: Yes
-- Created: 2026-03-04
-- MUST RUN FIRST before any other migrations

-- Create schema_migrations table if not exists
CREATE TABLE IF NOT EXISTS schema_migrations (
    version VARCHAR(100) PRIMARY KEY,
    applied_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    description TEXT,
    checksum VARCHAR(64),
    execution_time_ms INTEGER,
    applied_by VARCHAR(256) DEFAULT CURRENT_USER
);

-- Create index for faster lookups
CREATE INDEX IF NOT EXISTS idx_schema_migrations_applied_at ON schema_migrations(applied_at);

-- Log the creation
INSERT INTO schema_migrations (version, applied_at, description)
VALUES ('000', NOW(), 'CreateSchemaMigrationsTable: Migration tracking table')
ON CONFLICT (version) DO UPDATE SET
    applied_at = EXCLUDED.applied_at,
    description = EXCLUDED.description;
