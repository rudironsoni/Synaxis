-- Rollback: rollback_004.sql
-- Description: Rollback for 004_IdentitySchemaUpdates
-- Idempotent: Yes
-- Created: 2026-03-04

DO $$
BEGIN
    RAISE NOTICE 'Rolling back 004_IdentitySchemaUpdates...';
    
    -- Drop triggers
    DROP TRIGGER IF EXISTS trg_user_sessions_updated_at ON user_sessions;
    DROP TRIGGER IF EXISTS trg_user_webauthn_credentials_updated_at ON user_webauthn_credentials;
    DROP TRIGGER IF EXISTS trg_user_consent_records_updated_at ON user_consent_records;
    DROP TRIGGER IF EXISTS trg_user_profile_preferences_updated_at ON user_profile_preferences;
    
    -- Drop tables
    DROP TABLE IF EXISTS user_profile_preferences CASCADE;
    DROP TABLE IF EXISTS user_consent_records CASCADE;
    DROP TABLE IF EXISTS user_webauthn_credentials CASCADE;
    DROP TABLE IF EXISTS user_recovery_codes CASCADE;
    DROP TABLE IF EXISTS user_login_history CASCADE;
    DROP TABLE IF EXISTS user_security_events CASCADE;
    DROP TABLE IF EXISTS user_sessions CASCADE;
    
    -- Remove columns added to users table
    ALTER TABLE users DROP COLUMN IF EXISTS mfa_backup_codes;
    ALTER TABLE users DROP COLUMN IF EXISTS webauthn_enabled;
    ALTER TABLE users DROP COLUMN IF EXISTS password_change_reason;
    
    DELETE FROM schema_migrations WHERE version = '004';
    RAISE NOTICE 'Rollback 004 complete';
END $$;
