-- Validation Script: verify_data_integrity.sql
-- Description: Checksums, sample validation, and data integrity checks
-- Created: 2026-03-04

-- ============================================================================
-- Data Integrity Verification Report
-- ============================================================================
DO $$
DECLARE
    v_checksum VARCHAR(64);
    v_sample_count INTEGER := 10;
    v_total_mismatches INTEGER := 0;
    v_table_checksum TEXT;
    v_row RECORD;
BEGIN
    RAISE NOTICE '========================================';
    RAISE NOTICE 'DATA INTEGRITY VERIFICATION REPORT';
    RAISE NOTICE 'Generated: %', NOW();
    RAISE NOTICE '========================================';

    -- ============================================================================
    -- Section 1: Table Checksums (MD5 hash of row counts + data samples)
    -- ============================================================================
    RAISE NOTICE '';
    RAISE NOTICE '--- SECTION 1: TABLE CHECKSUMS ---';
    RAISE NOTICE '';

    -- Create checksums for critical tables
    SELECT 
        'model_configs:' || COUNT(*)::text || ':' ||
        COALESCE(SUM(EXTRACT(EPOCH FROM created_at)::bigint), 0)::text
    INTO v_table_checksum
    FROM model_configs;
    v_checksum := md5(v_table_checksum);
    RAISE NOTICE 'model_configs checksum: %', v_checksum;

    SELECT 
        'chat_templates:' || COUNT(*)::text || ':' ||
        COALESCE(SUM(EXTRACT(EPOCH FROM created_at)::bigint), 0)::text
    INTO v_table_checksum
    FROM chat_templates;
    v_checksum := md5(v_table_checksum);
    RAISE NOTICE 'chat_templates checksum: %', v_checksum;

    SELECT 
        'event_store:' || COUNT(*)::text || ':' ||
        COALESCE(MAX(global_position)::text, '0') || ':' ||
        COALESCE(SUM(LENGTH(event_data)::bigint), 0)::text
    INTO v_table_checksum
    FROM event_store;
    v_checksum := md5(v_table_checksum);
    RAISE NOTICE 'event_store checksum: %', v_checksum;

    SELECT 
        'subscriptions:' || COUNT(*)::text || ':' ||
        COALESCE(SUM(EXTRACT(EPOCH FROM current_period_start)::bigint), 0)::text
    INTO v_table_checksum
    FROM subscriptions;
    v_checksum := md5(v_table_checksum);
    RAISE NOTICE 'subscriptions checksum: %', v_checksum;

    SELECT 
        'invoices:' || COUNT(*)::text || ':' ||
        COALESCE(SUM(total)::text, '0') || ':' ||
        COALESCE(SUM(amount_paid)::text, '0')
    INTO v_table_checksum
    FROM invoices;
    v_checksum := md5(v_table_checksum);
    RAISE NOTICE 'invoices checksum: %', v_checksum;

    -- ============================================================================
    -- Section 2: Referential Integrity Sample Checks
    -- ============================================================================
    RAISE NOTICE '';
    RAISE NOTICE '--- SECTION 2: SAMPLE VALIDATION ---';
    RAISE NOTICE '';

    -- Sample check: inference_requests with valid model_configs
    RAISE NOTICE 'Sample check: inference_requests -> model_configs';
    FOR v_row IN 
        SELECT ir.id, ir.model_config_id, ir.request_id
        FROM inference_requests ir
        LEFT JOIN model_configs mc ON ir.model_config_id = mc.id
        WHERE ir.model_config_id IS NOT NULL AND mc.id IS NULL
        LIMIT v_sample_count
    LOOP
        RAISE WARNING '  Orphaned inference_request: id=%, model_config_id=%, request_id=%',
            v_row.id, v_row.model_config_id, v_row.request_id;
        v_total_mismatches := v_total_mismatches + 1;
    END LOOP;
    IF v_total_mismatches = 0 THEN
        RAISE NOTICE '  No orphaned records found';
    END IF;

    -- Sample check: subscriptions with valid billing_plans
    RAISE NOTICE '';
    RAISE NOTICE 'Sample check: subscriptions -> billing_plans';
    v_total_mismatches := 0;
    FOR v_row IN 
        SELECT s.id, s.plan_id, s.subscription_number
        FROM subscriptions s
        LEFT JOIN billing_plans bp ON s.plan_id = bp.id
        WHERE bp.id IS NULL
        LIMIT v_sample_count
    LOOP
        RAISE WARNING '  Orphaned subscription: id=%, plan_id=%, subscription_number=%',
            v_row.id, v_row.plan_id, v_row.subscription_number;
        v_total_mismatches := v_total_mismatches + 1;
    END LOOP;
    IF v_total_mismatches = 0 THEN
        RAISE NOTICE '  No orphaned records found';
    END IF;

    -- Sample check: invoice line items
    RAISE NOTICE '';
    RAISE NOTICE 'Sample check: invoice_line_items -> invoices';
    v_total_mismatches := 0;
    FOR v_row IN 
        SELECT ili.id, ili.invoice_id
        FROM invoice_line_items ili
        LEFT JOIN invoices i ON ili.invoice_id = i.id
        WHERE i.id IS NULL
        LIMIT v_sample_count
    LOOP
        RAISE WARNING '  Orphaned line item: id=%, invoice_id=%',
            v_row.id, v_row.invoice_id;
        v_total_mismatches := v_total_mismatches + 1;
    END LOOP;
    IF v_total_mismatches = 0 THEN
        RAISE NOTICE '  No orphaned records found';
    END IF;

    -- ============================================================================
    -- Section 3: Data Consistency Checks
    -- ============================================================================
    RAISE NOTICE '';
    RAISE NOTICE '--- SECTION 3: DATA CONSISTENCY CHECKS ---';
    RAISE NOTICE '';

    -- Check invoice totals match line items
    RAISE NOTICE 'Checking invoice totals consistency...';
    DECLARE
        v_mismatched_invoices INTEGER := 0;
    BEGIN
        FOR v_row IN 
            SELECT 
                i.id,
                i.invoice_number,
                i.subtotal as invoice_subtotal,
                COALESCE(SUM(ili.amount), 0) as calculated_subtotal
            FROM invoices i
            LEFT JOIN invoice_line_items ili ON i.id = ili.invoice_id
            GROUP BY i.id, i.invoice_number, i.subtotal
            HAVING ABS(i.subtotal - COALESCE(SUM(ili.amount), 0)) > 0.01
            LIMIT v_sample_count
        LOOP
            RAISE WARNING '  Mismatched invoice: id=%, number=%, invoice_subtotal=%, calculated=%',
                v_row.id, v_row.invoice_number, v_row.invoice_subtotal, v_row.calculated_subtotal;
            v_mismatched_invoices := v_mismatched_invoices + 1;
        END LOOP;
        
        IF v_mismatched_invoices = 0 THEN
            RAISE NOTICE '  Invoice totals are consistent with line items';
        ELSE
            RAISE WARNING '  Found % invoices with mismatched totals', v_mismatched_invoices;
        END IF;
    END;

    -- Check event store sequence
    RAISE NOTICE '';
    RAISE NOTICE 'Checking event store sequence integrity...';
    DECLARE
        v_sequence_gaps INTEGER := 0;
        v_max_position BIGINT;
        v_expected_count BIGINT;
        v_actual_count BIGINT;
    BEGIN
        SELECT MAX(global_position) INTO v_max_position FROM event_store;
        SELECT COUNT(*) INTO v_actual_count FROM event_store;
        v_expected_count := v_max_position;
        
        IF v_expected_count IS NOT NULL AND v_actual_count != v_expected_count THEN
            RAISE WARNING '  Event store sequence gap detected: expected % events, found %',
                v_expected_count, v_actual_count;
            v_sequence_gaps := 1;
        ELSE
            RAISE NOTICE '  Event store sequence is consistent';
        END IF;
    END;

    -- Check payment totals against invoices
    RAISE NOTICE '';
    RAISE NOTICE 'Checking payment totals against invoices...';
    DECLARE
        v_mismatched_payments INTEGER := 0;
    BEGIN
        FOR v_row IN 
            SELECT 
                i.id as invoice_id,
                i.invoice_number,
                i.amount_paid as invoice_paid,
                COALESCE(SUM(p.amount), 0) as payments_total
            FROM invoices i
            LEFT JOIN payments p ON i.id = p.invoice_id
            WHERE i.status = 'paid' OR i.amount_paid > 0
            GROUP BY i.id, i.invoice_number, i.amount_paid
            HAVING ABS(i.amount_paid - COALESCE(SUM(p.amount), 0)) > 0.01
            LIMIT v_sample_count
        LOOP
            RAISE WARNING '  Mismatched payments: invoice_id=%, number=%, amount_paid=%, payments_total=%',
                v_row.invoice_id, v_row.invoice_number, v_row.invoice_paid, v_row.payments_total;
            v_mismatched_payments := v_mismatched_payments + 1;
        END LOOP;
        
        IF v_mismatched_payments = 0 THEN
            RAISE NOTICE '  Payment totals are consistent with invoice amounts';
        ELSE
            RAISE WARNING '  Found % invoices with payment mismatches', v_mismatched_payments;
        END IF;
    END;

    -- ============================================================================
    -- Section 4: JSON Data Validation
    -- ============================================================================
    RAISE NOTICE '';
    RAISE NOTICE '--- SECTION 4: JSON DATA VALIDATION ---';
    RAISE NOTICE '';

    -- Check for invalid JSON in event_store
    DECLARE
        v_invalid_events INTEGER := 0;
    BEGIN
        SELECT COUNT(*) INTO v_invalid_events
        FROM event_store
        WHERE event_data IS NULL 
           OR jsonb_typeof(event_data) IS NULL
           OR event_data::text = '';
        
        IF v_invalid_events > 0 THEN
            RAISE WARNING '  Found % events with invalid JSON data', v_invalid_events;
        ELSE
            RAISE NOTICE '  Event store JSON data is valid';
        END IF;
    END;

    -- Check for invalid JSON in model_configs capabilities
    DECLARE
        v_invalid_configs INTEGER := 0;
    BEGIN
        SELECT COUNT(*) INTO v_invalid_configs
        FROM model_configs
        WHERE capabilities IS NULL 
           OR jsonb_typeof(capabilities) IS NULL;
        
        IF v_invalid_configs > 0 THEN
            RAISE WARNING '  Found % model_configs with invalid capabilities JSON', v_invalid_configs;
        ELSE
            RAISE NOTICE '  Model config JSON data is valid';
        END IF;
    END;

    -- ============================================================================
    -- Section 5: Temporal Data Validation
    -- ============================================================================
    RAISE NOTICE '';
    RAISE NOTICE '--- SECTION 5: TEMPORAL DATA VALIDATION ---';
    RAISE NOTICE '';

    -- Check for future dates
    DECLARE
        v_future_dates INTEGER := 0;
    BEGIN
        SELECT COUNT(*) INTO v_future_dates
        FROM invoices
        WHERE created_at > NOW() + INTERVAL '1 day';
        
        IF v_future_dates > 0 THEN
            RAISE WARNING '  Found % invoices with future created_at dates', v_future_dates;
        ELSE
            RAISE NOTICE '  No future invoice dates detected';
        END IF;
    END;

    -- Check for inconsistent date ranges
    DECLARE
        v_inconsistent_dates INTEGER := 0;
    BEGIN
        SELECT COUNT(*) INTO v_inconsistent_dates
        FROM subscriptions
        WHERE current_period_start > current_period_end
           OR trial_start > trial_end
           OR (canceled_at IS NOT NULL AND canceled_at < created_at);
        
        IF v_inconsistent_dates > 0 THEN
            RAISE WARNING '  Found % subscriptions with inconsistent date ranges', v_inconsistent_dates;
        ELSE
            RAISE NOTICE '  Subscription date ranges are consistent';
        END IF;
    END;

    -- ============================================================================
    -- Summary
    -- ============================================================================
    RAISE NOTICE '';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'DATA INTEGRITY VERIFICATION COMPLETE';
    RAISE NOTICE 'Review any warnings above for issues';
    RAISE NOTICE '========================================';

END $$;

-- ============================================================================
-- Sample Data Extraction (for manual verification)
-- ============================================================================
DO $$
BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '--- SAMPLE DATA FOR MANUAL VERIFICATION ---';
    RAISE NOTICE '';

    -- Sample model_configs
    RAISE NOTICE 'Sample model_configs:';
    RAISE NOTICE '%', (
        SELECT jsonb_pretty(jsonb_agg(to_jsonb(t)))
        FROM (
            SELECT id, model_id, provider_id, display_name, is_active
            FROM model_configs
            LIMIT 3
        ) t
    );

    RAISE NOTICE '';
    RAISE NOTICE 'Sample subscriptions:';
    RAISE NOTICE '%', (
        SELECT jsonb_pretty(jsonb_agg(to_jsonb(t)))
        FROM (
            SELECT id, subscription_number, status, current_period_start, current_period_end
            FROM subscriptions
            LIMIT 3
        ) t
    );

END $$;

-- ============================================================================
-- Migration Log Entry
-- ============================================================================
INSERT INTO schema_migrations (version, applied_at, description)
VALUES ('verify_data_integrity', NOW(), 'Validation: Checksums, sample validation, data integrity')
ON CONFLICT (version) DO UPDATE SET
    applied_at = EXCLUDED.applied_at,
    description = EXCLUDED.description;
