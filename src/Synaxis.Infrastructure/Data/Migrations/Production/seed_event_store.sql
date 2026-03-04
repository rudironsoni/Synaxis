-- Data Migration: seed_event_store.sql
-- Description: Initial event streams seeding for event sourcing
-- Idempotent: Yes
-- Created: 2026-03-04

-- ============================================================================
-- Migration Configuration
-- ============================================================================
DO $$
DECLARE
    v_organization_id UUID;
    v_system_user_id UUID := '00000000-0000-0000-0000-000000000001'::UUID;
    v_stream_id VARCHAR(512);
    v_event_id UUID;
BEGIN
    RAISE NOTICE 'Starting event store seeding...';

    -- Create system user if not exists (for system events)
    IF NOT EXISTS (SELECT 1 FROM users WHERE id = v_system_user_id) THEN
        INSERT INTO users (
            id, email, organization_id, first_name, last_name, 
            role, is_active, created_at, updated_at
        ) VALUES (
            v_system_user_id, 
            'system@synaxis.local',
            (SELECT id FROM organizations LIMIT 1),
            'System',
            'User',
            'System',
            true,
            NOW(),
            NOW()
        ) ON CONFLICT (id) DO NOTHING;
        RAISE NOTICE 'Created system user for event sourcing';
    END IF;

    -- ============================================================================
    -- Seed Organization Aggregate Streams
    -- ============================================================================
    RAISE NOTICE 'Seeding organization aggregate streams...';
    
    FOR v_organization_id IN 
        SELECT id FROM organizations WHERE deleted_at IS NULL
    LOOP
        v_stream_id := 'organization-' || v_organization_id::text;
        
        -- Create stream if not exists
        INSERT INTO event_streams (
            stream_id, aggregate_type, aggregate_id, current_version, metadata
        ) VALUES (
            v_stream_id,
            'Organization',
            v_organization_id::text,
            0,
            jsonb_build_object(
                'seeded_at', NOW(),
                'seeded_by', 'migration'
            )
        )
        ON CONFLICT (stream_id) DO NOTHING;
    END LOOP;
    
    RAISE NOTICE 'Organization streams seeded';

    -- ============================================================================
    -- Seed System Event Stream
    -- ============================================================================
    RAISE NOTICE 'Seeding system event stream...';
    
    INSERT INTO event_streams (
        stream_id, aggregate_type, aggregate_id, current_version, metadata
    ) VALUES (
        'system-events',
        'System',
        'system',
        0,
        jsonb_build_object(
            'description', 'System-wide events and migrations',
            'seeded_at', NOW()
        )
    )
    ON CONFLICT (stream_id) DO NOTHING;

    -- ============================================================================
    -- Seed Initial System Events
    -- ============================================================================
    RAISE NOTICE 'Seeding initial system events...';
    
    -- Migration Completed Event
    v_event_id := uuid_generate_v4();
    PERFORM append_event(
        'system-events',
        'System',
        'system',
        NULL, -- Let function determine expected version
        'System.MigrationCompleted',
        jsonb_build_object(
            'migrationVersion', '001',
            'migrationName', 'InitialInferenceSchema',
            'timestamp', NOW(),
            'description', 'Initial inference schema migration completed'
        ),
        jsonb_build_object(
            'eventId', v_event_id,
            'correlationId', v_event_id,
            'actorId', v_system_user_id,
            'actorType', 'system'
        )
    );

    -- Data Migration Event
    v_event_id := uuid_generate_v4();
    PERFORM append_event(
        'system-events',
        'System',
        'system',
        NULL,
        'System.DataMigrationStarted',
        jsonb_build_object(
            'migrationType', 'inference_gateway',
            'timestamp', NOW(),
            'description', 'Inference gateway data migration started'
        ),
        jsonb_build_object(
            'eventId', v_event_id,
            'correlationId', v_event_id,
            'actorId', v_system_user_id,
            'actorType', 'system'
        )
    );

    -- ============================================================================
    -- Seed Default Subscriptions for Event Store
    -- ============================================================================
    RAISE NOTICE 'Seeding event subscriptions...';
    
    INSERT INTO event_subscriptions (
        subscription_name,
        subscription_type,
        event_types,
        aggregate_types,
        last_processed_position,
        is_active,
        checkpoint_interval,
        metadata
    ) VALUES (
        'audit-log-projector',
        'catch_up',
        ARRAY['Organization.Created', 'Organization.Updated', 'Organization.Deleted', 
              'User.Created', 'User.Updated', 'User.Deleted'],
        ARRAY['Organization', 'User'],
        0,
        true,
        100,
        jsonb_build_object(
            'description', 'Projects events to audit log',
            'destination', 'audit_logs'
        )
    )
    ON CONFLICT (subscription_name) DO UPDATE SET
        event_types = EXCLUDED.event_types,
        aggregate_types = EXCLUDED.aggregate_types,
        updated_at = NOW();

    INSERT INTO event_subscriptions (
        subscription_name,
        subscription_type,
        event_types,
        aggregate_types,
        last_processed_position,
        is_active,
        checkpoint_interval,
        metadata
    ) VALUES (
        'notification-dispatcher',
        'catch_up',
        ARRAY['User.Invited', 'User.PasswordReset', 'Organization.Joined'],
        ARRAY['User', 'Organization'],
        0,
        true,
        50,
        jsonb_build_object(
            'description', 'Dispatches notifications for user events',
            'destination', 'notifications'
        )
    )
    ON CONFLICT (subscription_name) DO UPDATE SET
        event_types = EXCLUDED.event_types,
        aggregate_types = EXCLUDED.aggregate_types,
        updated_at = NOW();

    INSERT INTO event_subscriptions (
        subscription_name,
        subscription_type,
        event_types,
        aggregate_types,
        last_processed_position,
        is_active,
        checkpoint_interval,
        metadata
    ) VALUES (
        'analytics-aggregator',
        'catch_up',
        ARRAY['Inference.RequestCompleted', 'Inference.RequestFailed'],
        ARRAY['Inference'],
        0,
        true,
        500,
        jsonb_build_object(
            'description', 'Aggregates inference events for analytics',
            'destination', 'analytics'
        )
    )
    ON CONFLICT (subscription_name) DO UPDATE SET
        event_types = EXCLUDED.event_types,
        aggregate_types = EXCLUDED.aggregate_types,
        updated_at = NOW();

    -- ============================================================================
    -- Seed Default Projections
    -- ============================================================================
    RAISE NOTICE 'Seeding default projections...';
    
    -- Organization summary projection
    INSERT INTO event_projections (
        projection_name,
        projection_type,
        aggregate_type,
        aggregate_id,
        last_event_position,
        data,
        metadata
    )
    SELECT 
        'organization-summary',
        'read_model',
        'Organization',
        o.id::text,
        0,
        jsonb_build_object(
            'organizationId', o.id,
            'name', o.name,
            'slug', o.slug,
            'tier', o.tier,
            'isActive', o.is_active,
            'userCount', 0,
            'teamCount', 0,
            'lastUpdated', NOW()
        ),
        jsonb_build_object(
            'seeded', true,
            'seededAt', NOW()
        )
    FROM organizations o
    WHERE o.deleted_at IS NULL
    ON CONFLICT (projection_name, aggregate_type, aggregate_id) DO NOTHING;

    RAISE NOTICE 'Projections seeded';

    -- ============================================================================
    -- Migration Complete
    -- ============================================================================
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Event Store Seeding Complete';
    RAISE NOTICE '========================================';

END $$;

-- ============================================================================
-- Migration Log Entry
-- ============================================================================
INSERT INTO schema_migrations (version, applied_at, description)
VALUES ('seed_event_store', NOW(), 'Data Migration: Initial event streams and subscriptions seeding')
ON CONFLICT (version) DO UPDATE SET
    applied_at = EXCLUDED.applied_at,
    description = EXCLUDED.description;
