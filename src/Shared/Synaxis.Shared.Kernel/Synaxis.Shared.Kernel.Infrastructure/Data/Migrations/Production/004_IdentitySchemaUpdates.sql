-- Migration: 004_IdentitySchemaUpdates
-- Description: User aggregate updates for Identity domain
-- Idempotent: Yes
-- Created: 2026-03-04

-- ============================================================================
-- User Sessions Table (for tracking active sessions)
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    session_token_hash VARCHAR(512) NOT NULL UNIQUE,
    refresh_token_hash VARCHAR(512),
    device_id VARCHAR(256),
    device_name VARCHAR(256),
    device_type VARCHAR(50), -- desktop, mobile, tablet, etc.
    os_name VARCHAR(100),
    os_version VARCHAR(50),
    browser_name VARCHAR(100),
    browser_version VARCHAR(50),
    ip_address VARCHAR(45),
    ip_country VARCHAR(2),
    ip_city VARCHAR(100),
    is_trusted BOOLEAN DEFAULT FALSE,
    is_mfa_verified BOOLEAN DEFAULT FALSE,
    mfa_verified_at TIMESTAMP WITH TIME ZONE,
    started_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_activity_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    ended_at TIMESTAMP WITH TIME ZONE,
    end_reason VARCHAR(50), -- logout, expired, revoked, security
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- User Sessions indexes
CREATE INDEX IF NOT EXISTS idx_user_sessions_user_id ON user_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_user_sessions_org_id ON user_sessions(organization_id);
CREATE INDEX IF NOT EXISTS idx_user_sessions_active ON user_sessions(user_id, ended_at) WHERE ended_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_user_sessions_expires ON user_sessions(expires_at);
CREATE INDEX IF NOT EXISTS idx_user_sessions_device ON user_sessions(device_id);
CREATE INDEX IF NOT EXISTS idx_user_sessions_ip ON user_sessions(ip_address);

-- User Sessions constraints
ALTER TABLE user_sessions
    ADD CONSTRAINT IF NOT EXISTS chk_user_sessions_device_type CHECK (device_type IN ('desktop', 'mobile', 'tablet', 'smart_tv', 'console', 'wearable', 'other')),
    ADD CONSTRAINT IF NOT EXISTS chk_user_sessions_end_reason CHECK (end_reason IN ('logout', 'expired', 'revoked', 'security', 'concurrent_limit', 'unknown'));

-- ============================================================================
-- User Security Events Table (for security audit)
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_security_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL, -- login, logout, password_change, mfa_enable, etc.
    event_category VARCHAR(50) NOT NULL, -- authentication, authorization, account, security
    severity VARCHAR(20) NOT NULL DEFAULT 'info', -- info, warning, error, critical
    session_id UUID,
    ip_address VARCHAR(45),
    ip_country VARCHAR(2),
    user_agent TEXT,
    device_fingerprint VARCHAR(256),
    description TEXT,
    details JSONB DEFAULT '{}',
    risk_score INTEGER, -- 0-100
    risk_factors TEXT[] DEFAULT '{}',
    was_blocked BOOLEAN DEFAULT FALSE,
    block_reason VARCHAR(256),
    mitigation_action VARCHAR(100), -- mfa_required, email_verification, admin_review, etc.
    correlation_id UUID,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- User Security Events indexes
CREATE INDEX IF NOT EXISTS idx_user_security_events_user ON user_security_events(user_id);
CREATE INDEX IF NOT EXISTS idx_user_security_events_org ON user_security_events(organization_id);
CREATE INDEX IF NOT EXISTS idx_user_security_events_type ON user_security_events(event_type);
CREATE INDEX IF NOT EXISTS idx_user_security_events_category ON user_security_events(event_category);
CREATE INDEX IF NOT EXISTS idx_user_security_events_severity ON user_security_events(severity);
CREATE INDEX IF NOT EXISTS idx_user_security_events_created ON user_security_events(created_at);
CREATE INDEX IF NOT EXISTS idx_user_security_events_risk ON user_security_events(risk_score) WHERE risk_score IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_user_security_events_ip ON user_security_events(ip_address);

-- User Security Events GIN index
CREATE INDEX IF NOT EXISTS idx_user_security_events_details_gin ON user_security_events USING GIN(details);

-- User Security Events constraints
ALTER TABLE user_security_events
    ADD CONSTRAINT IF NOT EXISTS chk_user_security_events_type CHECK (event_type IN (
        'login_success', 'login_failure', 'logout', 'password_change', 'password_reset',
        'mfa_enabled', 'mfa_disabled', 'mfa_challenge', 'mfa_failure',
        'session_created', 'session_ended', 'session_revoked',
        'account_locked', 'account_unlocked', 'account_suspended',
        'email_verified', 'email_changed',
        'api_key_created', 'api_key_revoked',
        'permission_granted', 'permission_revoked',
        'suspicious_activity', 'impossible_travel', 'brute_force_detected'
    )),
    ADD CONSTRAINT IF NOT EXISTS chk_user_security_events_category CHECK (event_category IN ('authentication', 'authorization', 'account', 'security', 'api', 'system')),
    ADD CONSTRAINT IF NOT EXISTS chk_user_security_events_severity CHECK (severity IN ('info', 'warning', 'error', 'critical')),
    ADD CONSTRAINT IF NOT EXISTS chk_user_security_events_risk_score CHECK (risk_score >= 0 AND risk_score <= 100);

-- ============================================================================
-- User Login History Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_login_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    session_id UUID,
    was_successful BOOLEAN NOT NULL,
    failure_reason VARCHAR(100), -- invalid_credentials, account_locked, mfa_required, etc.
    ip_address VARCHAR(45) NOT NULL,
    ip_country VARCHAR(2),
    ip_city VARCHAR(100),
    ip_latitude DECIMAL(10, 8),
    ip_longitude DECIMAL(11, 8),
    user_agent TEXT,
    device_fingerprint VARCHAR(256),
    device_name VARCHAR(256),
    device_type VARCHAR(50),
    browser_name VARCHAR(100),
    os_name VARCHAR(100),
    is_new_device BOOLEAN DEFAULT FALSE,
    is_new_location BOOLEAN DEFAULT FALSE,
    is_suspicious BOOLEAN DEFAULT FALSE,
    mfa_method VARCHAR(50), -- totp, sms, email, backup_code, webauthn
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- User Login History indexes
CREATE INDEX IF NOT EXISTS idx_user_login_history_user ON user_login_history(user_id);
CREATE INDEX IF NOT EXISTS idx_user_login_history_org ON user_login_history(organization_id);
CREATE INDEX IF NOT EXISTS idx_user_login_history_success ON user_login_history(was_successful);
CREATE INDEX IF NOT EXISTS idx_user_login_history_created ON user_login_history(created_at);
CREATE INDEX IF NOT EXISTS idx_user_login_history_ip ON user_login_history(ip_address);

-- User Login History constraints
ALTER TABLE user_login_history
    ADD CONSTRAINT IF NOT EXISTS chk_login_history_failure_reason CHECK (failure_reason IN (
        'invalid_credentials', 'account_locked', 'account_disabled', 'account_suspended',
        'mfa_required', 'mfa_failed', 'email_not_verified', 'password_expired',
        'rate_limited', 'ip_blocked', 'suspicious_activity', 'unknown'
    )),
    ADD CONSTRAINT IF NOT EXISTS chk_login_history_mfa_method CHECK (mfa_method IN ('totp', 'sms', 'email', 'backup_code', 'webauthn', 'recovery_code', NULL));

-- ============================================================================
-- User Recovery Codes Table (for MFA backup)
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_recovery_codes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    code_hash VARCHAR(256) NOT NULL,
    code_hint VARCHAR(8) NOT NULL, -- First 4 chars of code
    is_used BOOLEAN DEFAULT FALSE,
    used_at TIMESTAMP WITH TIME ZONE,
    used_from_ip VARCHAR(45),
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- User Recovery Codes indexes
CREATE INDEX IF NOT EXISTS idx_user_recovery_codes_user ON user_recovery_codes(user_id);
CREATE INDEX IF NOT EXISTS idx_user_recovery_codes_unused ON user_recovery_codes(user_id, is_used) WHERE is_used = FALSE;
CREATE INDEX IF NOT EXISTS idx_user_recovery_codes_expires ON user_recovery_codes(expires_at);

-- Unique constraint to prevent duplicate codes per user
CREATE UNIQUE INDEX IF NOT EXISTS idx_user_recovery_codes_unique 
    ON user_recovery_codes(user_id, code_hash);

-- ============================================================================
-- User WebAuthn Credentials Table (for passkey support)
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_webauthn_credentials (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    credential_id VARCHAR(512) NOT NULL UNIQUE,
    public_key BYTEA NOT NULL,
    sign_count INTEGER DEFAULT 0,
    device_name VARCHAR(256),
    device_type VARCHAR(50),
    is_resident_key BOOLEAN DEFAULT FALSE,
    transports TEXT[] DEFAULT '{}', -- usb, nfc, ble, internal, hybrid
    aaguid VARCHAR(36),
    attestation_format VARCHAR(50),
    is_active BOOLEAN DEFAULT TRUE,
    last_used_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- WebAuthn Credentials indexes
CREATE INDEX IF NOT EXISTS idx_webauthn_user ON user_webauthn_credentials(user_id);
CREATE INDEX IF NOT EXISTS idx_webauthn_active ON user_webauthn_credentials(user_id, is_active) WHERE is_active = TRUE;
CREATE INDEX IF NOT EXISTS idx_webauthn_credential_id ON user_webauthn_credentials(credential_id);

-- WebAuthn Credentials constraints
ALTER TABLE user_webauthn_credentials
    ADD CONSTRAINT IF NOT EXISTS chk_webauthn_sign_count_non_negative CHECK (sign_count >= 0);

-- ============================================================================
-- User Consent Records Table (for GDPR/privacy compliance)
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_consent_records (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    consent_type VARCHAR(100) NOT NULL, -- terms_of_service, privacy_policy, marketing, cookies, etc.
    consent_version VARCHAR(50) NOT NULL,
    consent_language VARCHAR(10) DEFAULT 'en',
    is_granted BOOLEAN NOT NULL,
    granted_at TIMESTAMP WITH TIME ZONE,
    revoked_at TIMESTAMP WITH TIME ZONE,
    ip_address VARCHAR(45),
    user_agent TEXT,
    legal_basis VARCHAR(50), -- consent, contract, legal_obligation, vital_interests, public_task, legitimate_interests
    purpose_description TEXT,
    data_categories TEXT[] DEFAULT '{}',
    retention_period_days INTEGER,
    expires_at TIMESTAMP WITH TIME ZONE,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- User Consent Records indexes
CREATE INDEX IF NOT EXISTS idx_user_consent_user ON user_consent_records(user_id);
CREATE INDEX IF NOT EXISTS idx_user_consent_org ON user_consent_records(organization_id);
CREATE INDEX IF NOT EXISTS idx_user_consent_type ON user_consent_records(consent_type);
CREATE INDEX IF NOT EXISTS idx_user_consent_granted ON user_consent_records(user_id, is_granted);
CREATE INDEX IF NOT EXISTS idx_user_consent_expires ON user_consent_records(expires_at);

-- User Consent Records constraints
ALTER TABLE user_consent_records
    ADD CONSTRAINT IF NOT EXISTS chk_user_consent_type CHECK (consent_type IN (
        'terms_of_service', 'privacy_policy', 'marketing', 'cookies', 'analytics',
        'third_party_sharing', 'data_processing', 'biometric_data', 'location_data'
    )),
    ADD CONSTRAINT IF NOT EXISTS chk_user_consent_legal_basis CHECK (legal_basis IN (
        'consent', 'contract', 'legal_obligation', 'vital_interests', 'public_task', 'legitimate_interests'
    )),
    ADD CONSTRAINT IF NOT EXISTS chk_user_consent_retention_positive CHECK (retention_period_days > 0 OR retention_period_days IS NULL);

-- ============================================================================
-- User Profile Preferences Table (extended user settings)
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_profile_preferences (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL UNIQUE,
    organization_id UUID NOT NULL,
    
    -- UI Preferences
    theme VARCHAR(50) DEFAULT 'system',
    sidebar_collapsed BOOLEAN DEFAULT FALSE,
    default_landing_page VARCHAR(256) DEFAULT '/dashboard',
    date_format VARCHAR(50) DEFAULT 'MM/DD/YYYY',
    time_format VARCHAR(20) DEFAULT '12h',
    number_format VARCHAR(20) DEFAULT 'en-US',
    
    -- Notification Preferences
    email_notifications_enabled BOOLEAN DEFAULT TRUE,
    push_notifications_enabled BOOLEAN DEFAULT TRUE,
    slack_notifications_enabled BOOLEAN DEFAULT FALSE,
    weekly_digest_enabled BOOLEAN DEFAULT TRUE,
    security_alerts_enabled BOOLEAN DEFAULT TRUE,
    marketing_emails_enabled BOOLEAN DEFAULT FALSE,
    
    -- Communication Preferences
    preferred_contact_method VARCHAR(50) DEFAULT 'email',
    preferred_language VARCHAR(10) DEFAULT 'en',
    timezone VARCHAR(100) DEFAULT 'UTC',
    
    -- Privacy Preferences
    profile_visible_to_team BOOLEAN DEFAULT TRUE,
    show_activity_status BOOLEAN DEFAULT TRUE,
    allow_analytics BOOLEAN DEFAULT TRUE,
    allow_personalization BOOLEAN DEFAULT TRUE,
    
    -- Accessibility Preferences
    reduced_motion BOOLEAN DEFAULT FALSE,
    high_contrast BOOLEAN DEFAULT FALSE,
    font_size_scale DECIMAL(3,2) DEFAULT 1.0,
    
    -- Advanced Preferences
    custom_settings JSONB DEFAULT '{}',
    beta_features_enabled BOOLEAN DEFAULT FALSE,
    experimental_features TEXT[] DEFAULT '{}',
    
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- User Profile Preferences indexes
CREATE INDEX IF NOT EXISTS idx_user_profile_prefs_user ON user_profile_preferences(user_id);
CREATE INDEX IF NOT EXISTS idx_user_profile_prefs_org ON user_profile_preferences(organization_id);

-- User Profile Preferences constraints
ALTER TABLE user_profile_preferences
    ADD CONSTRAINT IF NOT EXISTS chk_user_profile_theme CHECK (theme IN ('light', 'dark', 'system', 'high-contrast')),
    ADD CONSTRAINT IF NOT EXISTS chk_user_profile_contact CHECK (preferred_contact_method IN ('email', 'sms', 'push', 'slack')),
    ADD CONSTRAINT IF NOT EXISTS chk_user_profile_font_scale CHECK (font_size_scale >= 0.75 AND font_size_scale <= 2.0);

-- ============================================================================
-- Add missing columns to existing Users table (if not present)
-- ============================================================================
DO $$
BEGIN
    -- Add MFA backup codes column if not exists
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'users' AND column_name = 'mfa_backup_codes') THEN
        ALTER TABLE users ADD COLUMN mfa_backup_codes TEXT[] DEFAULT '{}';
    END IF;

    -- Add WebAuthn enabled flag
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'users' AND column_name = 'webauthn_enabled') THEN
        ALTER TABLE users ADD COLUMN webauthn_enabled BOOLEAN DEFAULT FALSE;
    END IF;

    -- Add last password change reason
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'users' AND column_name = 'password_change_reason') THEN
        ALTER TABLE users ADD COLUMN password_change_reason VARCHAR(100);
    END IF;
END $$;

-- ============================================================================
-- Triggers
-- ============================================================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_user_sessions_updated_at') THEN
        CREATE TRIGGER trg_user_sessions_updated_at
        BEFORE UPDATE ON user_sessions
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_user_webauthn_credentials_updated_at') THEN
        CREATE TRIGGER trg_user_webauthn_credentials_updated_at
        BEFORE UPDATE ON user_webauthn_credentials
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_user_consent_records_updated_at') THEN
        CREATE TRIGGER trg_user_consent_records_updated_at
        BEFORE UPDATE ON user_consent_records
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_user_profile_preferences_updated_at') THEN
        CREATE TRIGGER trg_user_profile_preferences_updated_at
        BEFORE UPDATE ON user_profile_preferences
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;
END $$;

-- ============================================================================
-- Migration Completion Log
-- ============================================================================
INSERT INTO schema_migrations (version, applied_at, description)
VALUES ('004', NOW(), 'IdentitySchemaUpdates: user_sessions, user_security_events, user_login_history, user_recovery_codes, user_webauthn_credentials, user_consent_records, user_profile_preferences')
ON CONFLICT (version) DO UPDATE SET
    applied_at = EXCLUDED.applied_at,
    description = EXCLUDED.description;
