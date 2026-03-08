-- Rollback: rollback_001.sql
-- Description: Rollback for 001_InitialInferenceSchema
-- Idempotent: Yes
-- Created: 2026-03-04
-- WARNING: This will DELETE all data in these tables!

-- ============================================================================
-- Pre-Rollback Safety Check
-- ============================================================================
DO $$
DECLARE
    v_inference_requests_count BIGINT;
    v_confirm BOOLEAN := FALSE;
BEGIN
    -- Count records that will be deleted
    SELECT COUNT(*) INTO v_inference_requests_count FROM inference_requests;
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'ROLLBACK 001: InitialInferenceSchema';
    RAISE NOTICE '========================================';
    RAISE NOTICE '';
    RAISE NOTICE 'This rollback will DELETE the following data:';
    RAISE NOTICE '  - inference_requests: % rows', v_inference_requests_count;
    RAISE NOTICE '  - user_chat_preferences: % rows', (SELECT COUNT(*) FROM user_chat_preferences);
    RAISE NOTICE '  - chat_templates: % rows', (SELECT COUNT(*) FROM chat_templates);
    RAISE NOTICE '  - model_configs: % rows', (SELECT COUNT(*) FROM model_configs);
    RAISE NOTICE '';
    RAISE NOTICE 'WARNING: This action cannot be undone!';
    RAISE NOTICE '========================================';
    
    -- In production, you should require explicit confirmation
    -- For now, we'll abort unless explicitly allowed
    IF NOT v_confirm THEN
        RAISE EXCEPTION 'Rollback aborted. Set v_confirm := TRUE to proceed with rollback.';
    END IF;
END $$;

-- ============================================================================
-- Step 1: Drop Foreign Key Constraints
-- ============================================================================
DO $$
BEGIN
    -- Drop FK from inference_requests -> model_configs
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'fk_inference_model_config' AND table_name = 'inference_requests'
    ) THEN
        ALTER TABLE inference_requests DROP CONSTRAINT fk_inference_model_config;
        RAISE NOTICE 'Dropped FK: fk_inference_model_config';
    END IF;

    -- Drop FK from inference_requests -> chat_templates
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'fk_inference_chat_template' AND table_name = 'inference_requests'
    ) THEN
        ALTER TABLE inference_requests DROP CONSTRAINT fk_inference_chat_template;
        RAISE NOTICE 'Dropped FK: fk_inference_chat_template';
    END IF;

    -- Drop FK from user_chat_preferences -> model_configs
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'fk_user_chat_prefs_model_config' AND table_name = 'user_chat_preferences'
    ) THEN
        ALTER TABLE user_chat_preferences DROP CONSTRAINT fk_user_chat_prefs_model_config;
        RAISE NOTICE 'Dropped FK: fk_user_chat_prefs_model_config';
    END IF;

    -- Drop FK from user_chat_preferences -> chat_templates
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'fk_user_chat_prefs_chat_template' AND table_name = 'user_chat_preferences'
    ) THEN
        ALTER TABLE user_chat_preferences DROP CONSTRAINT fk_user_chat_prefs_chat_template;
        RAISE NOTICE 'Dropped FK: fk_user_chat_prefs_chat_template';
    END IF;
END $$;

-- ============================================================================
-- Step 2: Drop Triggers
-- ============================================================================
DO $$
BEGIN
    -- Drop triggers
    DROP TRIGGER IF EXISTS trg_chat_templates_updated_at ON chat_templates;
    DROP TRIGGER IF EXISTS trg_model_configs_updated_at ON model_configs;
    DROP TRIGGER IF EXISTS trg_user_chat_preferences_updated_at ON user_chat_preferences;
    RAISE NOTICE 'Dropped triggers';
END $$;

-- ============================================================================
-- Step 3: Drop Tables
-- ============================================================================
DROP TABLE IF EXISTS inference_requests CASCADE;
DROP TABLE IF EXISTS user_chat_preferences CASCADE;
DROP TABLE IF EXISTS chat_templates CASCADE;
DROP TABLE IF EXISTS model_configs CASCADE;

RAISE NOTICE 'Dropped tables: inference_requests, user_chat_preferences, chat_templates, model_configs';

-- ============================================================================
-- Step 4: Remove Migration Log Entry
-- ============================================================================
DELETE FROM schema_migrations WHERE version = '001';

RAISE NOTICE '========================================';
RAISE NOTICE 'ROLLBACK 001 COMPLETE';
RAISE NOTICE '========================================';
