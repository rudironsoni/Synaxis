-- Data Migration: migrate_inference_gateway_data.sql
-- Description: Migrate old Gateway data to new Inference schema
-- Idempotent: Yes
-- Created: 2026-03-04

-- ============================================================================
-- Migration Configuration
-- ============================================================================
DO $$
DECLARE
    v_migration_batch_size INTEGER := 1000;
    v_total_migrated INTEGER := 0;
    v_batch_migrated INTEGER := 0;
BEGIN
    RAISE NOTICE 'Starting inference gateway data migration...';
    RAISE NOTICE 'Batch size: %', v_migration_batch_size;

    -- ============================================================================
    -- Step 1: Migrate Model Configurations
    -- ============================================================================
    RAISE NOTICE 'Step 1: Migrating model configurations...';
    
    -- Check if old gateway_models table exists
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'gateway_models') THEN
        INSERT INTO model_configs (
            id,
            organization_id,
            model_id,
            provider_id,
            display_name,
            description,
            max_tokens,
            temperature,
            top_p,
            input_price_per_1k,
            output_price_per_1k,
            is_active,
            is_default,
            capabilities,
            metadata,
            created_at,
            updated_at
        )
        SELECT 
            COALESCE(gm.id, uuid_generate_v4()),
            gm.organization_id,
            gm.model_identifier,
            gm.provider,
            COALESCE(gm.display_name, gm.model_identifier),
            gm.description,
            COALESCE(gm.max_tokens, 4096),
            COALESCE(gm.temperature, 0.7),
            COALESCE(gm.top_p, 1.0),
            COALESCE(gm.input_cost_per_1k_tokens, 0.0),
            COALESCE(gm.output_cost_per_1k_tokens, 0.0),
            COALESCE(gm.is_enabled, true),
            COALESCE(gm.is_default, false),
            COALESCE(gm.capabilities::jsonb, '[]'::jsonb),
            jsonb_build_object(
                'legacy_model_id', gm.legacy_id,
                'migrated_from', 'gateway_models',
                'migrated_at', NOW()
            ),
            COALESCE(gm.created_at, NOW()),
            COALESCE(gm.updated_at, NOW())
        FROM gateway_models gm
        WHERE gm.deleted_at IS NULL
          AND NOT EXISTS (
              SELECT 1 FROM model_configs mc 
              WHERE mc.organization_id = gm.organization_id 
                AND mc.model_id = gm.model_identifier 
                AND mc.provider_id = gm.provider
          )
        ON CONFLICT (organization_id, model_id, provider_id) DO NOTHING;
        
        GET DIAGNOSTICS v_batch_migrated = ROW_COUNT;
        v_total_migrated := v_total_migrated + v_batch_migrated;
        RAISE NOTICE 'Migrated % model configurations', v_batch_migrated;
    ELSE
        RAISE NOTICE 'gateway_models table not found, skipping model config migration';
    END IF;

    -- ============================================================================
    -- Step 2: Migrate Inference Requests
    -- ============================================================================
    RAISE NOTICE 'Step 2: Migrating inference requests...';
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'gateway_requests') THEN
        -- Migrate in batches
        LOOP
            INSERT INTO inference_requests (
                id,
                request_id,
                organization_id,
                user_id,
                team_id,
                virtual_key_id,
                model_config_id,
                model,
                provider,
                request_content,
                response_content,
                input_tokens,
                output_tokens,
                total_tokens,
                cost,
                duration_ms,
                queue_time_ms,
                status,
                error_message,
                error_code,
                request_headers,
                response_headers,
                metadata,
                user_region,
                processed_region,
                cross_border_transfer,
                created_at,
                completed_at
            )
            SELECT 
                COALESCE(gr.id, uuid_generate_v4()),
                COALESCE(gr.request_id, 'mig-' || gr.id::text),
                gr.organization_id,
                gr.user_id,
                gr.team_id,
                gr.api_key_id,
                -- Try to find matching model_config
                (SELECT mc.id FROM model_configs mc 
                 WHERE mc.organization_id = gr.organization_id 
                   AND mc.model_id = gr.model 
                   AND mc.provider_id = gr.provider 
                 LIMIT 1),
                gr.model,
                gr.provider,
                COALESCE(gr.prompt, gr.request_content, ''),
                COALESCE(gr.response_text, gr.response_content),
                COALESCE(gr.prompt_tokens, 0),
                COALESCE(gr.completion_tokens, 0),
                COALESCE(gr.total_tokens, COALESCE(gr.prompt_tokens, 0) + COALESCE(gr.completion_tokens, 0)),
                COALESCE(gr.cost, 0.0),
                COALESCE(gr.duration_ms, 0),
                COALESCE(gr.queue_time_ms, 0),
                CASE 
                    WHEN gr.status = 'success' THEN 'completed'
                    WHEN gr.status = 'error' THEN 'failed'
                    WHEN gr.status = 'pending' THEN 'pending'
                    ELSE COALESCE(gr.status, 'completed')
                END,
                gr.error_message,
                gr.error_code,
                COALESCE(gr.request_headers, '{}'::jsonb),
                COALESCE(gr.response_headers, '{}'::jsonb),
                jsonb_build_object(
                    'legacy_request_id', gr.legacy_id,
                    'migrated_from', 'gateway_requests',
                    'migrated_at', NOW(),
                    'original_status', gr.status
                ),
                gr.user_region,
                gr.processed_region,
                COALESCE(gr.cross_border, false),
                COALESCE(gr.created_at, NOW()),
                gr.completed_at
            FROM gateway_requests gr
            WHERE gr.id NOT IN (
                SELECT ir.id FROM inference_requests ir WHERE ir.id IS NOT NULL
            )
            ORDER BY gr.created_at
            LIMIT v_migration_batch_size;
            
            GET DIAGNOSTICS v_batch_migrated = ROW_COUNT;
            v_total_migrated := v_total_migrated + v_batch_migrated;
            
            EXIT WHEN v_batch_migrated = 0;
            RAISE NOTICE 'Migrated batch of % inference requests (total: %)', v_batch_migrated, v_total_migrated;
            
            -- Commit every batch to prevent long transactions
            COMMIT;
        END LOOP;
        
        RAISE NOTICE 'Completed inference requests migration. Total migrated: %', v_total_migrated;
    ELSE
        RAISE NOTICE 'gateway_requests table not found, skipping inference requests migration';
    END IF;

    -- ============================================================================
    -- Step 3: Migrate Chat Templates (if applicable)
    -- ============================================================================
    RAISE NOTICE 'Step 3: Migrating chat templates...';
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'gateway_templates') THEN
        INSERT INTO chat_templates (
            id,
            organization_id,
            name,
            description,
            system_prompt,
            user_prompt_template,
            variables,
            is_active,
            is_system,
            created_by,
            created_at,
            updated_at
        )
        SELECT 
            COALESCE(gt.id, uuid_generate_v4()),
            gt.organization_id,
            gt.name,
            gt.description,
            COALESCE(gt.system_prompt, 'You are a helpful assistant.'),
            COALESCE(gt.user_template, '{{input}}'),
            COALESCE(gt.variables::jsonb, '{}'::jsonb),
            COALESCE(gt.is_active, true),
            COALESCE(gt.is_system, false),
            gt.created_by,
            COALESCE(gt.created_at, NOW()),
            COALESCE(gt.updated_at, NOW())
        FROM gateway_templates gt
        WHERE gt.deleted_at IS NULL
          AND NOT EXISTS (
              SELECT 1 FROM chat_templates ct 
              WHERE ct.organization_id = gt.organization_id 
                AND ct.name = gt.name
          )
        ON CONFLICT (id) DO NOTHING;
        
        GET DIAGNOSTICS v_batch_migrated = ROW_COUNT;
        RAISE NOTICE 'Migrated % chat templates', v_batch_migrated;
    ELSE
        RAISE NOTICE 'gateway_templates table not found, skipping chat templates migration';
    END IF;

    -- ============================================================================
    -- Step 4: Migrate User Chat Preferences
    -- ============================================================================
    RAISE NOTICE 'Step 4: Migrating user chat preferences...';
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'gateway_user_settings') THEN
        INSERT INTO user_chat_preferences (
            id,
            user_id,
            organization_id,
            default_model_config_id,
            default_chat_template_id,
            preferred_temperature,
            preferred_max_tokens,
            auto_save_chats,
            enable_streaming,
            theme_preference,
            language_preference,
            timezone,
            notification_settings,
            privacy_settings,
            custom_settings,
            created_at,
            updated_at
        )
        SELECT 
            COALESCE(gus.id, uuid_generate_v4()),
            gus.user_id,
            gus.organization_id,
            -- Try to find default model config
            (SELECT mc.id FROM model_configs mc 
             WHERE mc.organization_id = gus.organization_id 
               AND mc.is_default = true 
             LIMIT 1),
            -- Try to find default template
            (SELECT ct.id FROM chat_templates ct 
             WHERE ct.organization_id = gus.organization_id 
               AND ct.is_active = true 
             ORDER BY ct.created_at DESC 
             LIMIT 1),
            COALESCE(gus.default_temperature, 0.7),
            COALESCE(gus.default_max_tokens, 4096),
            COALESCE(gus.auto_save, true),
            COALESCE(gus.streaming_enabled, true),
            COALESCE(gus.theme, 'system'),
            COALESCE(gus.language, 'en'),
            COALESCE(gus.timezone, 'UTC'),
            COALESCE(gus.notification_settings::jsonb, '{}'::jsonb),
            COALESCE(gus.privacy_settings::jsonb, '{}'::jsonb),
            jsonb_build_object(
                'legacy_settings', COALESCE(gus.custom_settings::jsonb, '{}'::jsonb),
                'migrated_from', 'gateway_user_settings',
                'migrated_at', NOW()
            ),
            COALESCE(gus.created_at, NOW()),
            COALESCE(gus.updated_at, NOW())
        FROM gateway_user_settings gus
        WHERE NOT EXISTS (
            SELECT 1 FROM user_chat_preferences ucp 
            WHERE ucp.user_id = gus.user_id 
              AND ucp.organization_id = gus.organization_id
        )
        ON CONFLICT (user_id, organization_id) DO UPDATE SET
            updated_at = EXCLUDED.updated_at,
            custom_settings = EXCLUDED.custom_settings;
        
        GET DIAGNOSTICS v_batch_migrated = ROW_COUNT;
        RAISE NOTICE 'Migrated % user chat preferences', v_batch_migrated;
    ELSE
        RAISE NOTICE 'gateway_user_settings table not found, skipping user preferences migration';
    END IF;

    -- ============================================================================
    -- Migration Complete
    -- ============================================================================
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Inference Gateway Data Migration Complete';
    RAISE NOTICE 'Total records processed: %', v_total_migrated;
    RAISE NOTICE '========================================';

END $$;

-- ============================================================================
-- Migration Log Entry
-- ============================================================================
INSERT INTO schema_migrations (version, applied_at, description)
VALUES ('data_migration_inference', NOW(), 'Data Migration: Old Gateway -> New Inference schema')
ON CONFLICT (version) DO UPDATE SET
    applied_at = EXCLUDED.applied_at,
    description = EXCLUDED.description;
