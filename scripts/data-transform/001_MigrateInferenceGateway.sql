-- =============================================================================
-- Data Transformation: Migrate InferenceGateway to Inference
-- Description: Transforms old InferenceGateway data to new Inference schema
-- Created: 2026-03-04
-- Idempotent: Yes
-- =============================================================================

-- =============================================================================
-- PRE-MIGRATION VALIDATION
-- =============================================================================

DO $$
BEGIN
    -- Check if old tables exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables
                   WHERE table_name = 'inference_gateway_requests') THEN
        RAISE NOTICE 'Old table inference_gateway_requests not found - skipping migration';
        RETURN;
    END IF;

    RAISE NOTICE 'Starting InferenceGateway to Inference migration...';
END $$;

-- Create backup of old data before transformation
CREATE TABLE IF NOT EXISTS inference_gateway_requests_backup AS
SELECT * FROM inference_gateway_requests WHERE 1=0;

-- Backup data if not already backed up
INSERT INTO inference_gateway_requests_backup
SELECT * FROM inference_gateway_requests
WHERE NOT EXISTS (SELECT 1 FROM inference_gateway_requests_backup LIMIT 1);

-- =============================================================================
-- MIGRATION: INFERENCE REQUESTS
-- =============================================================================

-- Transform old inference requests to new format
INSERT INTO requests (
    id,
    request_id,
    organization_id,
    user_id,
    virtual_key_id,
    team_id,
    user_region,
    processed_region,
    stored_region,
    cross_border_transfer,
    transfer_legal_basis,
    transfer_purpose,
    transfer_timestamp,
    model,
    provider,
    input_tokens,
    output_tokens,
    cost,
    duration_ms,
    queue_time_ms,
    request_size_bytes,
    response_size_bytes,
    status_code,
    client_ip_address,
    user_agent,
    request_headers,
    created_at,
    completed_at
)
SELECT
    COALESCE(igr.new_uuid, uuid_generate_v4()),
    COALESCE(igr.request_uuid, uuid_generate_v4()),
    igr.org_id,
    igr.user_id,
    igr.api_key_id,
    igr.team_id,
    COALESCE(igr.user_region, 'us-east-1'),
    COALESCE(igr.processed_region, igr.user_region, 'us-east-1'),
    COALESCE(igr.stored_region, igr.user_region, 'us-east-1'),
    COALESCE(igr.cross_border_transfer, false),
    CASE WHEN igr.cross_border_transfer = true THEN 'consent' ELSE NULL END,
    CASE WHEN igr.cross_border_transfer = true THEN 'inference' ELSE NULL END,
    CASE WHEN igr.cross_border_transfer = true THEN igr.created_at ELSE NULL END,
    igr.model_name,
    igr.provider_name,
    COALESCE(igr.input_tokens, 0),
    COALESCE(igr.output_tokens, 0),
    COALESCE(igr.cost_usd, 0),
    COALESCE(igr.latency_ms, 0),
    COALESCE(igr.queue_time_ms, 0),
    COALESCE(igr.request_size_bytes, 0),
    COALESCE(igr.response_size_bytes, 0),
    COALESCE(igr.status_code, 200),
    COALESCE(igr.client_ip, '0.0.0.0'),
    COALESCE(igr.user_agent, ''),
    COALESCE(igr.headers::jsonb, '{}'),
    igr.created_at,
    igr.completed_at
FROM inference_gateway_requests igr
LEFT JOIN requests r ON r.request_id = igr.request_uuid
WHERE r.id IS NULL  -- Only insert if not already migrated
    AND igr.created_at >= CURRENT_DATE - INTERVAL '90 days'; -- Only recent data

-- =============================================================================
-- MIGRATION: INFERENCE METADATA
-- =============================================================================

-- Create mapping table for old to new IDs if it doesn't exist
CREATE TABLE IF NOT EXISTS migration_inference_id_mapping (
    old_id VARCHAR(255) PRIMARY KEY,
    new_id UUID NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    migrated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Migrate model configurations
INSERT INTO model_configs (
    id,
    model_id,
    provider_id,
    display_name,
    description,
    settings,
    pricing,
    tenant_id,
    is_active,
    created_at
)
SELECT
    uuid_generate_v4(),
    imc.model_name,
    imc.provider_name,
    COALESCE(imc.display_name, imc.model_name),
    imc.description,
    jsonb_build_object(
        'maxTokens', COALESCE(imc.max_tokens, 2048),
        'temperature', COALESCE(imc.temperature, 0.7),
        'topP', COALESCE(imc.top_p, 1.0)
    ),
    jsonb_build_object(
        'inputPricePer1K', COALESCE(imc.input_price_per_1k, 0.001),
        'outputPricePer1K', COALESCE(imc.output_price_per_1k, 0.002)
    ),
    imc.org_id,
    COALESCE(imc.is_active, true),
    COALESCE(imc.created_at, NOW())
FROM inference_model_configs imc
LEFT JOIN model_configs mc ON mc.model_id = imc.model_name AND mc.provider_id = imc.provider_name
WHERE mc.id IS NULL;

-- =============================================================================
-- MIGRATION: SPEND LOGS FROM INFERENCE DATA
-- =============================================================================

INSERT INTO spend_logs (
    id,
    organization_id,
    team_id,
    virtual_key_id,
    request_id,
    amount_usd,
    model,
    provider,
    tokens,
    region,
    created_at
)
SELECT
    uuid_generate_v4(),
    igr.org_id,
    igr.team_id,
    igr.api_key_id,
    r.id,
    COALESCE(igr.cost_usd, 0),
    igr.model_name,
    igr.provider_name,
    COALESCE(igr.input_tokens, 0) + COALESCE(igr.output_tokens, 0),
    COALESCE(igr.processed_region, 'us-east-1'),
    igr.created_at
FROM inference_gateway_requests igr
JOIN requests r ON r.request_id = igr.request_uuid
LEFT JOIN spend_logs sl ON sl.request_id = r.id
WHERE sl.id IS NULL
    AND igr.cost_usd IS NOT NULL
    AND igr.cost_usd > 0;

-- =============================================================================
-- POST-MIGRATION VALIDATION
-- =============================================================================

-- Count migrated records
DO $$
DECLARE
    v_migrated_requests INTEGER;
    v_migrated_models INTEGER;
    v_migrated_spend INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_migrated_requests
    FROM requests r
    JOIN inference_gateway_requests igr ON igr.request_uuid = r.request_id;

    SELECT COUNT(*) INTO v_migrated_models
    FROM model_configs mc
    WHERE EXISTS (SELECT 1 FROM inference_model_configs imc
                  WHERE imc.model_name = mc.model_id
                    AND imc.provider_name = mc.provider_id);

    SELECT COUNT(*) INTO v_migrated_spend
    FROM spend_logs sl
    JOIN requests r ON r.id = sl.request_id
    WHERE r.request_id IN (SELECT request_uuid FROM inference_gateway_requests);

    RAISE NOTICE 'Migration Summary:';
    RAISE NOTICE '  - Requests migrated: %', v_migrated_requests;
    RAISE NOTICE '  - Model configs migrated: %', v_migrated_models;
    RAISE NOTICE '  - Spend logs created: %', v_migrated_spend;
END $$;

-- =============================================================================
-- DATA INTEGRITY CHECKS
-- =============================================================================

-- Check for orphaned records
DO $$
DECLARE
    v_orphaned INTEGER;
BEGIN
    -- Check requests without valid organizations
    SELECT COUNT(*) INTO v_orphaned
    FROM requests r
    WHERE NOT EXISTS (SELECT 1 FROM organizations o WHERE o.id = r.organization_id);

    IF v_orphaned > 0 THEN
        RAISE WARNING 'Found % requests with invalid organization references', v_orphaned;
    END IF;

    -- Check spend_logs without valid requests
    SELECT COUNT(*) INTO v_orphaned
    FROM spend_logs sl
    WHERE sl.request_id IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM requests r WHERE r.id = sl.request_id);

    IF v_orphaned > 0 THEN
        RAISE WARNING 'Found % spend_logs with invalid request references', v_orphaned;
    END IF;
END $$;

-- =============================================================================
-- MIGRATION METADATA
-- =============================================================================

-- Record this transformation
INSERT INTO __migrations_history (migration_id, migration_name, checksum, is_idempotent)
VALUES ('DT001', 'MigrateInferenceGatewayToInference', MD5(pg_read_file('/scripts/data-transform/001_MigrateInferenceGateway.sql')::text), true)
ON CONFLICT (migration_id) DO NOTHING;

-- =============================================================================
-- CLEANUP (Optional - run after verification)
-- =============================================================================

-- Note: Uncomment below only after full verification
-- DROP TABLE IF EXISTS inference_gateway_requests;
-- DROP TABLE IF EXISTS inference_model_configs;
-- DROP TABLE IF EXISTS inference_gateway_requests_backup;
