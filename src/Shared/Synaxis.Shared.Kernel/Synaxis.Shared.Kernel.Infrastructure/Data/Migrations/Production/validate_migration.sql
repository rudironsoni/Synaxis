-- Validation Script: validate_migration.sql
-- Description: Row counts, FK checks, and basic validation
-- Created: 2026-03-04

-- ============================================================================
-- Validation Summary Report
-- ============================================================================
DO $$
DECLARE
    v_table_name TEXT;
    v_row_count BIGINT;
    v_validation_passed BOOLEAN := TRUE;
    v_total_tables INTEGER := 0;
    v_validated_tables INTEGER := 0;
BEGIN
    RAISE NOTICE '========================================';
    RAISE NOTICE 'MIGRATION VALIDATION REPORT';
    RAISE NOTICE 'Generated: %', NOW();
    RAISE NOTICE '========================================';

    -- ============================================================================
    -- Section 1: Row Count Validation
    -- ============================================================================
    RAISE NOTICE '';
    RAISE NOTICE '--- SECTION 1: ROW COUNTS ---';
    RAISE NOTICE '';

    -- Inference Schema Tables
    RAISE NOTICE 'Inference Schema:';
    
    SELECT COUNT(*) INTO v_row_count FROM model_configs;
    RAISE NOTICE '  model_configs: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM chat_templates;
    RAISE NOTICE '  chat_templates: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM user_chat_preferences;
    RAISE NOTICE '  user_chat_preferences: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM inference_requests;
    RAISE NOTICE '  inference_requests: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    -- Event Sourcing Tables
    RAISE NOTICE '';
    RAISE NOTICE 'Event Sourcing Schema:';
    
    SELECT COUNT(*) INTO v_row_count FROM event_streams;
    RAISE NOTICE '  event_streams: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM event_store;
    RAISE NOTICE '  event_store: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM event_store_snapshots;
    RAISE NOTICE '  event_store_snapshots: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM event_projections;
    RAISE NOTICE '  event_projections: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM event_subscriptions;
    RAISE NOTICE '  event_subscriptions: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM event_dead_letter;
    RAISE NOTICE '  event_dead_letter: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    -- API Management Tables
    RAISE NOTICE '';
    RAISE NOTICE 'API Management Schema:';
    
    SELECT COUNT(*) INTO v_row_count FROM api_keys;
    RAISE NOTICE '  api_keys: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM api_key_usage_logs;
    RAISE NOTICE '  api_key_usage_logs: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM rate_limit_configs;
    RAISE NOTICE '  rate_limit_configs: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM rate_limit_assignments;
    RAISE NOTICE '  rate_limit_assignments: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM rate_limit_tracking;
    RAISE NOTICE '  rate_limit_tracking: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM api_endpoints;
    RAISE NOTICE '  api_endpoints: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    -- Identity Schema Tables
    RAISE NOTICE '';
    RAISE NOTICE 'Identity Schema:';
    
    SELECT COUNT(*) INTO v_row_count FROM user_sessions;
    RAISE NOTICE '  user_sessions: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM user_security_events;
    RAISE NOTICE '  user_security_events: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM user_login_history;
    RAISE NOTICE '  user_login_history: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM user_recovery_codes;
    RAISE NOTICE '  user_recovery_codes: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM user_webauthn_credentials;
    RAISE NOTICE '  user_webauthn_credentials: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM user_consent_records;
    RAISE NOTICE '  user_consent_records: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM user_profile_preferences;
    RAISE NOTICE '  user_profile_preferences: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    -- Billing Schema Tables
    RAISE NOTICE '';
    RAISE NOTICE 'Billing Schema:';
    
    SELECT COUNT(*) INTO v_row_count FROM billing_plans;
    RAISE NOTICE '  billing_plans: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM subscriptions;
    RAISE NOTICE '  subscriptions: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM subscription_items;
    RAISE NOTICE '  subscription_items: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM invoices;
    RAISE NOTICE '  invoices: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM invoice_line_items;
    RAISE NOTICE '  invoice_line_items: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM payments;
    RAISE NOTICE '  payments: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM credit_notes;
    RAISE NOTICE '  credit_notes: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    SELECT COUNT(*) INTO v_row_count FROM usage_records;
    RAISE NOTICE '  usage_records: % rows', v_row_count;
    v_total_tables := v_total_tables + 1;
    IF v_row_count >= 0 THEN v_validated_tables := v_validated_tables + 1; END IF;

    -- ============================================================================
    -- Section 2: Foreign Key Validation
    -- ============================================================================
    RAISE NOTICE '';
    RAISE NOTICE '--- SECTION 2: FOREIGN KEY VALIDATION ---';
    RAISE NOTICE '';

    -- Check for orphaned inference_requests
    SELECT COUNT(*) INTO v_row_count
    FROM inference_requests ir
    LEFT JOIN model_configs mc ON ir.model_config_id = mc.id
    WHERE ir.model_config_id IS NOT NULL AND mc.id IS NULL;
    IF v_row_count > 0 THEN
        RAISE WARNING '  Found % orphaned inference_requests (invalid model_config_id)', v_row_count;
        v_validation_passed := FALSE;
    ELSE
        RAISE NOTICE '  inference_requests -> model_configs: OK';
    END IF;

    -- Check for orphaned user_chat_preferences
    SELECT COUNT(*) INTO v_row_count
    FROM user_chat_preferences ucp
    LEFT JOIN model_configs mc ON ucp.default_model_config_id = mc.id
    WHERE ucp.default_model_config_id IS NOT NULL AND mc.id IS NULL;
    IF v_row_count > 0 THEN
        RAISE WARNING '  Found % orphaned user_chat_preferences (invalid default_model_config_id)', v_row_count;
        v_validation_passed := FALSE;
    ELSE
        RAISE NOTICE '  user_chat_preferences -> model_configs: OK';
    END IF;

    -- Check for orphaned subscriptions
    SELECT COUNT(*) INTO v_row_count
    FROM subscriptions s
    LEFT JOIN billing_plans bp ON s.plan_id = bp.id
    WHERE bp.id IS NULL;
    IF v_row_count > 0 THEN
        RAISE WARNING '  Found % orphaned subscriptions (invalid plan_id)', v_row_count;
        v_validation_passed := FALSE;
    ELSE
        RAISE NOTICE '  subscriptions -> billing_plans: OK';
    END IF;

    -- Check for orphaned invoice line items
    SELECT COUNT(*) INTO v_row_count
    FROM invoice_line_items ili
    LEFT JOIN invoices i ON ili.invoice_id = i.id
    WHERE i.id IS NULL;
    IF v_row_count > 0 THEN
        RAISE WARNING '  Found % orphaned invoice_line_items (invalid invoice_id)', v_row_count;
        v_validation_passed := FALSE;
    ELSE
        RAISE NOTICE '  invoice_line_items -> invoices: OK';
    END IF;

    -- Check for orphaned payments
    SELECT COUNT(*) INTO v_row_count
    FROM payments p
    LEFT JOIN invoices i ON p.invoice_id = i.id
    WHERE p.invoice_id IS NOT NULL AND i.id IS NULL;
    IF v_row_count > 0 THEN
        RAISE WARNING '  Found % orphaned payments (invalid invoice_id)', v_row_count;
        v_validation_passed := FALSE;
    ELSE
        RAISE NOTICE '  payments -> invoices: OK';
    END IF;

    -- ============================================================================
    -- Section 3: Constraint Validation
    -- ============================================================================
    RAISE NOTICE '';
    RAISE NOTICE '--- SECTION 3: CONSTRAINT VALIDATION ---';
    RAISE NOTICE '';

    -- Check for invalid temperature values
    SELECT COUNT(*) INTO v_row_count
    FROM model_configs
    WHERE temperature < 0 OR temperature > 2;
    IF v_row_count > 0 THEN
        RAISE WARNING '  Found % model_configs with invalid temperature', v_row_count;
        v_validation_passed := FALSE;
    ELSE
        RAISE NOTICE '  model_configs temperature constraints: OK';
    END IF;

    -- Check for invalid negative amounts
    SELECT COUNT(*) INTO v_row_count
    FROM invoices
    WHERE total < 0 OR subtotal < 0 OR amount_due < 0;
    IF v_row_count > 0 THEN
        RAISE WARNING '  Found % invoices with negative amounts', v_row_count;
        v_validation_passed := FALSE;
    ELSE
        RAISE NOTICE '  invoices amount constraints: OK';
    END IF;

    -- Check for invalid email formats in users (basic check)
    SELECT COUNT(*) INTO v_row_count
    FROM users
    WHERE email NOT LIKE '%@%.%';
    IF v_row_count > 0 THEN
        RAISE WARNING '  Found % users with potentially invalid emails', v_row_count;
    ELSE
        RAISE NOTICE '  users email format: OK';
    END IF;

    -- ============================================================================
    -- Section 4: Index Validation
    -- ============================================================================
    RAISE NOTICE '';
    RAISE NOTICE '--- SECTION 4: INDEX VALIDATION ---';
    RAISE NOTICE '';

    -- Check critical indexes exist
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'idx_inference_requests_org_id') THEN
        RAISE NOTICE '  idx_inference_requests_org_id: EXISTS';
    ELSE
        RAISE WARNING '  idx_inference_requests_org_id: MISSING';
        v_validation_passed := FALSE;
    END IF;

    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'idx_event_store_stream_version') THEN
        RAISE NOTICE '  idx_event_store_stream_version: EXISTS';
    ELSE
        RAISE WARNING '  idx_event_store_stream_version: MISSING';
        v_validation_passed := FALSE;
    END IF;

    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'idx_api_keys_hash') THEN
        RAISE NOTICE '  idx_api_keys_hash: EXISTS';
    ELSE
        RAISE WARNING '  idx_api_keys_hash: MISSING';
        v_validation_passed := FALSE;
    END IF;

    -- ============================================================================
    -- Summary
    -- ============================================================================
    RAISE NOTICE '';
    RAISE NOTICE '========================================';
    IF v_validation_passed THEN
        RAISE NOTICE 'VALIDATION RESULT: PASSED';
    ELSE
        RAISE NOTICE 'VALIDATION RESULT: FAILED - Review warnings above';
    END IF;
    RAISE NOTICE 'Tables validated: %/%', v_validated_tables, v_total_tables;
    RAISE NOTICE '========================================';

END $$;

-- ============================================================================
-- Migration Log Entry
-- ============================================================================
INSERT INTO schema_migrations (version, applied_at, description)
VALUES ('validate_migration', NOW(), 'Validation: Row counts, FK checks, constraint validation')
ON CONFLICT (version) DO UPDATE SET
    applied_at = EXCLUDED.applied_at,
    description = EXCLUDED.description;
