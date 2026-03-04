-- Data Migration: migrate_user_data.sql
-- Description: Migrate legacy users to Identity aggregate
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
    v_legacy_users_count INTEGER := 0;
BEGIN
    RAISE NOTICE 'Starting user data migration...';
    RAISE NOTICE 'Batch size: %', v_migration_batch_size;

    -- ============================================================================
    -- Step 1: Migrate Legacy Users to Users Table
    -- ============================================================================
    RAISE NOTICE 'Step 1: Migrating legacy users...';
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'legacy_users') THEN
        -- Count legacy users first
        SELECT COUNT(*) INTO v_legacy_users_count FROM legacy_users WHERE deleted_at IS NULL;
        RAISE NOTICE 'Found % legacy users to migrate', v_legacy_users_count;
        
        -- Migrate in batches
        LOOP
            WITH migrated_users AS (
                INSERT INTO users (
                    id,
                    organization_id,
                    email,
                    email_verified_at,
                    password_hash,
                    data_residency_region,
                    created_in_region,
                    first_name,
                    last_name,
                    avatar_url,
                    timezone,
                    locale,
                    role,
                    cross_border_consent_given,
                    cross_border_consent_date,
                    cross_border_consent_version,
                    mfa_enabled,
                    mfa_secret,
                    mfa_backup_codes,
                    last_login_at,
                    failed_login_attempts,
                    locked_until,
                    password_expires_at,
                    failed_password_change_attempts,
                    password_change_locked_until,
                    password_changed_at,
                    must_change_password,
                    is_active,
                    created_at,
                    updated_at,
                    deleted_at,
                    privacy_consent
                )
                SELECT 
                    COALESCE(lu.id, uuid_generate_v4()),
                    COALESCE(lu.organization_id, (SELECT id FROM organizations LIMIT 1)),
                    LOWER(TRIM(lu.email)),
                    lu.email_verified_at,
                    COALESCE(lu.password_hash, lu.legacy_password_hash),
                    COALESCE(lu.region, 'us-east-1'),
                    COALESCE(lu.region, 'us-east-1'),
                    lu.first_name,
                    lu.last_name,
                    lu.avatar_url,
                    COALESCE(lu.timezone, 'UTC'),
                    COALESCE(lu.locale, 'en'),
                    COALESCE(lu.role, 'Member'),
                    COALESCE(lu.cross_border_consent, false),
                    lu.cross_border_consent_date,
                    lu.cross_border_consent_version,
                    COALESCE(lu.mfa_enabled, false),
                    lu.mfa_secret,
                    COALESCE(lu.mfa_backup_codes, ARRAY[]::text[]),
                    lu.last_login_at,
                    COALESCE(lu.failed_login_attempts, 0),
                    lu.locked_until,
                    lu.password_expires_at,
                    COALESCE(lu.failed_password_change_attempts, 0),
                    lu.password_change_locked_until,
                    lu.password_changed_at,
                    COALESCE(lu.must_change_password, false),
                    COALESCE(lu.is_active, true),
                    COALESCE(lu.created_at, NOW()),
                    COALESCE(lu.updated_at, NOW()),
                    lu.deleted_at,
                    jsonb_build_object(
                        'legacy_user_id', lu.legacy_id,
                        'migrated_from', 'legacy_users',
                        'migrated_at', NOW(),
                        'original_role', lu.legacy_role
                    )
                FROM legacy_users lu
                WHERE lu.deleted_at IS NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM users u WHERE u.email = LOWER(TRIM(lu.email))
                  )
                ORDER BY lu.created_at
                LIMIT v_migration_batch_size
                RETURNING id
            )
            SELECT COUNT(*) INTO v_batch_migrated FROM migrated_users;
            
            v_total_migrated := v_total_migrated + v_batch_migrated;
            
            EXIT WHEN v_batch_migrated = 0;
            RAISE NOTICE 'Migrated batch of % users (total: %/% processed)', 
                v_batch_migrated, v_total_migrated, v_legacy_users_count;
            
            COMMIT;
        END LOOP;
        
        RAISE NOTICE 'Completed user migration. Total migrated: %', v_total_migrated;
    ELSE
        RAISE NOTICE 'legacy_users table not found, skipping user migration';
    END IF;

    -- ============================================================================
    -- Step 2: Migrate Legacy User Sessions
    -- ============================================================================
    RAISE NOTICE 'Step 2: Migrating user sessions...';
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'legacy_sessions') THEN
        INSERT INTO user_sessions (
            id,
            user_id,
            organization_id,
            session_token_hash,
            refresh_token_hash,
            device_id,
            device_name,
            device_type,
            os_name,
            os_version,
            browser_name,
            browser_version,
            ip_address,
            ip_country,
            is_trusted,
            is_mfa_verified,
            mfa_verified_at,
            started_at,
            last_activity_at,
            expires_at,
            ended_at,
            end_reason,
            metadata
        )
        SELECT 
            COALESCE(ls.id, uuid_generate_v4()),
            ls.user_id,
            ls.organization_id,
            COALESCE(ls.token_hash, ls.session_token_hash),
            ls.refresh_token_hash,
            ls.device_id,
            COALESCE(ls.device_name, 'Unknown Device'),
            COALESCE(ls.device_type, 'desktop'),
            ls.os_name,
            ls.os_version,
            ls.browser_name,
            ls.browser_version,
            ls.ip_address,
            ls.ip_country,
            COALESCE(ls.is_trusted, false),
            COALESCE(ls.mfa_verified, false),
            ls.mfa_verified_at,
            COALESCE(ls.started_at, ls.created_at, NOW()),
            COALESCE(ls.last_activity_at, ls.updated_at, NOW()),
            COALESCE(ls.expires_at, ls.token_expires_at, NOW() + INTERVAL '7 days'),
            ls.ended_at,
            CASE 
                WHEN ls.ended_at IS NOT NULL THEN COALESCE(ls.end_reason, 'logout')
                ELSE NULL
            END,
            jsonb_build_object(
                'legacy_session_id', ls.legacy_id,
                'migrated_from', 'legacy_sessions',
                'migrated_at', NOW()
            )
        FROM legacy_sessions ls
        WHERE ls.user_id IN (SELECT id FROM users)
          AND NOT EXISTS (
              SELECT 1 FROM user_sessions us 
              WHERE us.session_token_hash = COALESCE(ls.token_hash, ls.session_token_hash)
          )
        ON CONFLICT (session_token_hash) DO NOTHING;
        
        GET DIAGNOSTICS v_batch_migrated = ROW_COUNT;
        RAISE NOTICE 'Migrated % user sessions', v_batch_migrated;
    ELSE
        RAISE NOTICE 'legacy_sessions table not found, skipping sessions migration';
    END IF;

    -- ============================================================================
    -- Step 3: Migrate User Login History
    -- ============================================================================
    RAISE NOTICE 'Step 3: Migrating login history...';
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'legacy_login_history') THEN
        INSERT INTO user_login_history (
            id,
            user_id,
            organization_id,
            session_id,
            was_successful,
            failure_reason,
            ip_address,
            ip_country,
            ip_city,
            ip_latitude,
            ip_longitude,
            user_agent,
            device_fingerprint,
            device_name,
            device_type,
            browser_name,
            os_name,
            is_new_device,
            is_new_location,
            is_suspicious,
            mfa_method,
            created_at
        )
        SELECT 
            COALESCE(llh.id, uuid_generate_v4()),
            llh.user_id,
            llh.organization_id,
            llh.session_id,
            COALESCE(llh.success, llh.was_successful, true),
            llh.failure_reason,
            COALESCE(llh.ip_address, '0.0.0.0'),
            llh.ip_country,
            llh.ip_city,
            llh.ip_latitude,
            llh.ip_longitude,
            llh.user_agent,
            llh.device_fingerprint,
            llh.device_name,
            COALESCE(llh.device_type, 'unknown'),
            llh.browser_name,
            llh.os_name,
            COALESCE(llh.is_new_device, false),
            COALESCE(llh.is_new_location, false),
            COALESCE(llh.is_suspicious, false),
            llh.mfa_method,
            COALESCE(llh.created_at, llh.timestamp, NOW())
        FROM legacy_login_history llh
        WHERE llh.user_id IN (SELECT id FROM users)
          AND NOT EXISTS (
              SELECT 1 FROM user_login_history ulh 
              WHERE ulh.user_id = llh.user_id 
                AND ulh.created_at = COALESCE(llh.created_at, llh.timestamp)
                AND ulh.ip_address = COALESCE(llh.ip_address, '0.0.0.0')
          )
        ON CONFLICT DO NOTHING;
        
        GET DIAGNOSTICS v_batch_migrated = ROW_COUNT;
        RAISE NOTICE 'Migrated % login history records', v_batch_migrated;
    ELSE
        RAISE NOTICE 'legacy_login_history table not found, skipping login history migration';
    END IF;

    -- ============================================================================
    -- Step 4: Migrate User Security Events
    -- ============================================================================
    RAISE NOTICE 'Step 4: Migrating security events...';
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'legacy_security_events') THEN
        INSERT INTO user_security_events (
            id,
            user_id,
            organization_id,
            event_type,
            event_category,
            severity,
            session_id,
            ip_address,
            ip_country,
            user_agent,
            device_fingerprint,
            description,
            details,
            risk_score,
            risk_factors,
            was_blocked,
            block_reason,
            mitigation_action,
            correlation_id,
            created_at
        )
        SELECT 
            COALESCE(lse.id, uuid_generate_v4()),
            lse.user_id,
            lse.organization_id,
            CASE 
                WHEN lse.event_type LIKE 'login%' THEN 'login_success'
                WHEN lse.event_type LIKE 'auth%' THEN 'login_success'
                WHEN lse.event_type LIKE 'password%' THEN 'password_change'
                WHEN lse.event_type LIKE 'mfa%' THEN 'mfa_enabled'
                WHEN lse.event_type LIKE 'suspicious%' THEN 'suspicious_activity'
                ELSE COALESCE(lse.event_type, 'login_success')
            END,
            COALESCE(lse.event_category, 'authentication'),
            COALESCE(lse.severity, 'info'),
            lse.session_id,
            lse.ip_address,
            lse.ip_country,
            lse.user_agent,
            lse.device_fingerprint,
            lse.description,
            COALESCE(lse.details::jsonb, jsonb_build_object(
                'legacy_event_id', lse.legacy_id,
                'migrated_from', 'legacy_security_events'
            )),
            lse.risk_score,
            COALESCE(lse.risk_factors, ARRAY[]::text[]),
            COALESCE(lse.was_blocked, false),
            lse.block_reason,
            lse.mitigation_action,
            lse.correlation_id,
            COALESCE(lse.created_at, lse.timestamp, NOW())
        FROM legacy_security_events lse
        WHERE lse.user_id IN (SELECT id FROM users)
        ON CONFLICT DO NOTHING;
        
        GET DIAGNOSTICS v_batch_migrated = ROW_COUNT;
        RAISE NOTICE 'Migrated % security events', v_batch_migrated;
    ELSE
        RAISE NOTICE 'legacy_security_events table not found, skipping security events migration';
    END IF;

    -- ============================================================================
    -- Step 5: Migrate User Consent Records
    -- ============================================================================
    RAISE NOTICE 'Step 5: Migrating consent records...';
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'legacy_user_consents') THEN
        INSERT INTO user_consent_records (
            id,
            user_id,
            organization_id,
            consent_type,
            consent_version,
            consent_language,
            is_granted,
            granted_at,
            revoked_at,
            ip_address,
            user_agent,
            legal_basis,
            purpose_description,
            data_categories,
            retention_period_days,
            expires_at,
            metadata,
            created_at,
            updated_at
        )
        SELECT 
            COALESCE(luc.id, uuid_generate_v4()),
            luc.user_id,
            luc.organization_id,
            COALESCE(luc.consent_type, 'terms_of_service'),
            COALESCE(luc.version, luc.consent_version, '1.0'),
            COALESCE(luc.language, 'en'),
            COALESCE(luc.granted, luc.is_granted, true),
            COALESCE(luc.granted_at, luc.timestamp, NOW()),
            luc.revoked_at,
            luc.ip_address,
            luc.user_agent,
            COALESCE(luc.legal_basis, 'consent'),
            luc.purpose_description,
            COALESCE(luc.data_categories, ARRAY[]::text[]),
            luc.retention_period_days,
            luc.expires_at,
            jsonb_build_object(
                'legacy_consent_id', luc.legacy_id,
                'migrated_from', 'legacy_user_consents',
                'migrated_at', NOW()
            ),
            COALESCE(luc.created_at, NOW()),
            COALESCE(luc.updated_at, NOW())
        FROM legacy_user_consents luc
        WHERE luc.user_id IN (SELECT id FROM users)
        ON CONFLICT DO NOTHING;
        
        GET DIAGNOSTICS v_batch_migrated = ROW_COUNT;
        RAISE NOTICE 'Migrated % consent records', v_batch_migrated;
    ELSE
        RAISE NOTICE 'legacy_user_consents table not found, skipping consent records migration';
    END IF;

    -- ============================================================================
    -- Step 6: Migrate User Recovery Codes
    -- ============================================================================
    RAISE NOTICE 'Step 6: Migrating recovery codes...';
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'legacy_recovery_codes') THEN
        INSERT INTO user_recovery_codes (
            id,
            user_id,
            code_hash,
            code_hint,
            is_used,
            used_at,
            used_from_ip,
            expires_at,
            created_at
        )
        SELECT 
            COALESCE(lrc.id, uuid_generate_v4()),
            lrc.user_id,
            COALESCE(lrc.code_hash, lrc.hashed_code),
            COALESCE(lrc.code_hint, SUBSTRING(COALESCE(lrc.plain_code, '****'), 1, 4) || '**'),
            COALESCE(lrc.used, lrc.is_used, false),
            lrc.used_at,
            lrc.used_from_ip,
            COALESCE(lrc.expires_at, NOW() + INTERVAL '1 year'),
            COALESCE(lrc.created_at, NOW())
        FROM legacy_recovery_codes lrc
        WHERE lrc.user_id IN (SELECT id FROM users)
          AND NOT EXISTS (
              SELECT 1 FROM user_recovery_codes urc 
              WHERE urc.user_id = lrc.user_id 
                AND urc.code_hash = COALESCE(lrc.code_hash, lrc.hashed_code)
          )
        ON CONFLICT DO NOTHING;
        
        GET DIAGNOSTICS v_batch_migrated = ROW_COUNT;
        RAISE NOTICE 'Migrated % recovery codes', v_batch_migrated;
    ELSE
        RAISE NOTICE 'legacy_recovery_codes table not found, skipping recovery codes migration';
    END IF;

    -- ============================================================================
    -- Step 7: Create Default Profile Preferences for Migrated Users
    -- ============================================================================
    RAISE NOTICE 'Step 7: Creating default profile preferences...';
    
    INSERT INTO user_profile_preferences (
        id,
        user_id,
        organization_id,
        theme,
        timezone,
        preferred_language,
        notification_settings,
        custom_settings,
        created_at,
        updated_at
    )
    SELECT 
        uuid_generate_v4(),
        u.id,
        u.organization_id,
        'system',
        COALESCE(u.timezone, 'UTC'),
        COALESCE(u.locale, 'en'),
        jsonb_build_object(
            'email_notifications', true,
            'security_alerts', true
        ),
        jsonb_build_object(
            'migrated_from_users_table', true,
            'migrated_at', NOW()
        ),
        NOW(),
        NOW()
    FROM users u
    WHERE NOT EXISTS (
        SELECT 1 FROM user_profile_preferences upp 
        WHERE upp.user_id = u.id
    );
    
    GET DIAGNOSTICS v_batch_migrated = ROW_COUNT;
    RAISE NOTICE 'Created % default profile preferences', v_batch_migrated;

    -- ============================================================================
    -- Migration Complete
    -- ============================================================================
    RAISE NOTICE '========================================';
    RAISE NOTICE 'User Data Migration Complete';
    RAISE NOTICE 'Total users migrated: %', v_total_migrated;
    RAISE NOTICE '========================================';

END $$;

-- ============================================================================
-- Migration Log Entry
-- ============================================================================
INSERT INTO schema_migrations (version, applied_at, description)
VALUES ('data_migration_users', NOW(), 'Data Migration: Legacy Users -> Identity aggregate')
ON CONFLICT (version) DO UPDATE SET
    applied_at = EXCLUDED.applied_at,
    description = EXCLUDED.description;
