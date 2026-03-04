-- =============================================================================
-- Data Transformation: Migrate Users to New Identity Model
-- Description: Transforms user data to the new Identity aggregate structure
-- Created: 2026-03-04
-- Idempotent: Yes
-- =============================================================================

-- =============================================================================
-- PRE-MIGRATION SETUP
-- =============================================================================

-- Create backup table
CREATE TABLE IF NOT EXISTS users_legacy_backup AS
SELECT * FROM users WHERE 1=0;

-- Backup current user data if not already backed up
INSERT INTO users_legacy_backup
SELECT * FROM users
WHERE NOT EXISTS (SELECT 1 FROM users_legacy_backup LIMIT 1);

-- =============================================================================
-- MIGRATION: USER PROFILE DATA
-- =============================================================================

-- Add new columns if they don't exist (for idempotency)
DO $$
BEGIN
    -- Add MFA-related columns
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'users' AND column_name = 'mfa_secret_encrypted') THEN
        ALTER TABLE users ADD COLUMN mfa_secret_encrypted TEXT;
    END IF;

    -- Add password history reference
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'users' AND column_name = 'password_version') THEN
        ALTER TABLE users ADD COLUMN password_version INTEGER DEFAULT 1;
    END IF;

    -- Add identity provider reference
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'users' AND column_name = 'identity_provider') THEN
        ALTER TABLE users ADD COLUMN identity_provider VARCHAR(50) DEFAULT 'local';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'users' AND column_name = 'external_id') THEN
        ALTER TABLE users ADD COLUMN external_id TEXT;
    END IF;
END $$;

-- =============================================================================
-- MIGRATION: CREATE IDENTITY AGGREGATE RECORDS
-- =============================================================================

-- Create IdentityAggregate table if it doesn't exist
CREATE TABLE IF NOT EXISTS identity_aggregates (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    aggregate_version BIGINT NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    email_verified BOOLEAN NOT NULL DEFAULT false,
    phone_verified BOOLEAN NOT NULL DEFAULT false,
    identity_verified BOOLEAN NOT NULL DEFAULT false,
    verification_level VARCHAR(20) DEFAULT 'basic',
    consent_version VARCHAR(20),
    consent_accepted_at TIMESTAMPTZ,
    gdpr_data_export_requested_at TIMESTAMPTZ,
    gdpr_data_export_completed_at TIMESTAMPTZ,
    gdpr_deletion_requested_at TIMESTAMPTZ,
    gdpr_deletion_completed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_identity_aggregates_user ON identity_aggregates(user_id);
CREATE INDEX IF NOT EXISTS idx_identity_aggregates_status ON identity_aggregates(status);

-- Migrate user data to identity aggregates
INSERT INTO identity_aggregates (
    id,
    user_id,
    aggregate_version,
    status,
    email_verified,
    consent_version,
    consent_accepted_at,
    created_at,
    updated_at
)
SELECT
    uuid_generate_v4(),
    u.id,
    1,
    CASE
        WHEN u.is_active = false THEN 'suspended'
        WHEN u.locked_until IS NOT NULL AND u.locked_until > NOW() THEN 'locked'
        WHEN u.email_verified_at IS NULL THEN 'pending_verification'
        ELSE 'active'
    END,
    u.email_verified_at IS NOT NULL,
    u.cross_border_consent_version,
    u.cross_border_consent_date,
    u.created_at,
    u.updated_at
FROM users u
LEFT JOIN identity_aggregates ia ON ia.user_id = u.id
WHERE ia.id IS NULL;

-- =============================================================================
-- MIGRATION: USER ROLES TO NEW MODEL
-- =============================================================================

-- Create UserRoles table for normalized role storage
CREATE TABLE IF NOT EXISTS user_roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_name VARCHAR(50) NOT NULL,
    scope_type VARCHAR(50) NOT NULL DEFAULT 'organization', -- 'organization', 'team', 'global'
    scope_id UUID,
    granted_by UUID REFERENCES users(id) ON DELETE SET NULL,
    granted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB DEFAULT '{}',

    CONSTRAINT uq_user_roles_user_role_scope UNIQUE (user_id, role_name, scope_type, scope_id)
);

CREATE INDEX IF NOT EXISTS idx_user_roles_user ON user_roles(user_id);
CREATE INDEX IF NOT EXISTS idx_user_roles_scope ON user_roles(scope_type, scope_id);
CREATE INDEX IF NOT EXISTS idx_user_roles_active ON user_roles(is_active);

-- Migrate existing user roles
INSERT INTO user_roles (
    user_id,
    role_name,
    scope_type,
    scope_id,
    granted_at,
    is_active
)
SELECT
    u.id,
    CASE u.role
        WHEN 'Admin' THEN 'OrgAdmin'
        WHEN 'Member' THEN 'Member'
        ELSE COALESCE(u.role, 'Member')
    END,
    'organization',
    u.organization_id,
    u.created_at,
    u.is_active
FROM users u
LEFT JOIN user_roles ur ON ur.user_id = u.id AND ur.scope_type = 'organization'
WHERE ur.id IS NULL;

-- Migrate team memberships to roles
INSERT INTO user_roles (
    user_id,
    role_name,
    scope_type,
    scope_id,
    granted_at,
    granted_by,
    is_active
)
SELECT
    tm.user_id,
    CASE tm.role
        WHEN 'TeamAdmin' THEN 'TeamAdmin'
        ELSE 'TeamMember'
    END,
    'team',
    tm.team_id,
    tm.joined_at,
    tm.invited_by,
    true
FROM team_memberships tm
LEFT JOIN user_roles ur ON ur.user_id = tm.user_id
    AND ur.scope_type = 'team'
    AND ur.scope_id = tm.team_id
WHERE ur.id IS NULL;

-- =============================================================================
-- MIGRATION: USER SESSIONS
-- =============================================================================

-- Create UserSessions table for session management
CREATE TABLE IF NOT EXISTS user_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    session_token_hash TEXT NOT NULL UNIQUE,
    ip_address INET,
    user_agent TEXT,
    device_fingerprint TEXT,
    geo_location JSONB,
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_active_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    ended_at TIMESTAMPTZ,
    end_reason VARCHAR(50),
    is_active BOOLEAN NOT NULL DEFAULT true
);

CREATE INDEX IF NOT EXISTS idx_user_sessions_user ON user_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_user_sessions_token ON user_sessions(session_token_hash);
CREATE INDEX IF NOT EXISTS idx_user_sessions_active ON user_sessions(is_active, expires_at);

-- Migrate refresh tokens to sessions
INSERT INTO user_sessions (
    user_id,
    session_token_hash,
    started_at,
    expires_at,
    is_active
)
SELECT
    rt.user_id,
    rt.token_hash,
    rt.created_at,
    rt.expires_at,
    rt.is_revoked = false
FROM refresh_tokens rt
LEFT JOIN user_sessions us ON us.session_token_hash = rt.token_hash
WHERE us.id IS NULL;

-- =============================================================================
-- MIGRATION: USER AUDIT LOG ENTRIES
-- =============================================================================

-- Create identity audit log table
CREATE TABLE IF NOT EXISTS identity_audit_log (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    action VARCHAR(100) NOT NULL,
    category VARCHAR(50) NOT NULL,
    details JSONB NOT NULL DEFAULT '{}',
    ip_address INET,
    user_agent TEXT,
    session_id UUID REFERENCES user_sessions(id) ON DELETE SET NULL,
    success BOOLEAN NOT NULL DEFAULT true,
    failure_reason TEXT,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_identity_audit_user ON identity_audit_log(user_id);
CREATE INDEX IF NOT EXISTS idx_identity_audit_action ON identity_audit_log(action);
CREATE INDEX IF NOT EXISTS idx_identity_audit_timestamp ON identity_audit_log(timestamp);

-- Seed initial audit entries for migrated users
INSERT INTO identity_audit_log (
    user_id,
    action,
    category,
    details,
    timestamp
)
SELECT
    ia.user_id,
    'identity_migrated',
    'migration',
    jsonb_build_object(
        'from_version', 'legacy',
        'to_version', 'v2',
        'migration_date', CURRENT_DATE
    ),
    NOW()
FROM identity_aggregates ia
LEFT JOIN identity_audit_log ial ON ial.user_id = ia.user_id AND ial.action = 'identity_migrated'
WHERE ial.id IS NULL;

-- =============================================================================
-- POST-MIGRATION VALIDATION
-- =============================================================================

DO $$
DECLARE
    v_total_users INTEGER;
    v_migrated_identities INTEGER;
    v_migrated_roles INTEGER;
    v_migrated_sessions INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_total_users FROM users;
    SELECT COUNT(*) INTO v_migrated_identities FROM identity_aggregates;
    SELECT COUNT(*) INTO v_migrated_roles FROM user_roles;
    SELECT COUNT(*) INTO v_migrated_sessions FROM user_sessions;

    RAISE NOTICE 'Identity Migration Summary:';
    RAISE NOTICE '  - Total users: %', v_total_users;
    RAISE NOTICE '  - Identity aggregates created: %', v_migrated_identities;
    RAISE NOTICE '  - User roles migrated: %', v_migrated_roles;
    RAISE NOTICE '  - Sessions migrated: %', v_migrated_sessions;

    -- Verify all users have identity aggregates
    IF v_migrated_identities != v_total_users THEN
        RAISE WARNING 'Mismatch: % users but % identity aggregates', v_total_users, v_migrated_identities;
    END IF;
END $$;

-- =============================================================================
-- DATA INTEGRITY CHECKS
-- =============================================================================

DO $$
DECLARE
    v_orphaned INTEGER;
    v_invalid_roles INTEGER;
BEGIN
    -- Check for orphaned identity aggregates
    SELECT COUNT(*) INTO v_orphaned
    FROM identity_aggregates ia
    WHERE NOT EXISTS (SELECT 1 FROM users u WHERE u.id = ia.user_id);

    IF v_orphaned > 0 THEN
        RAISE WARNING 'Found % orphaned identity aggregates', v_orphaned;
    END IF;

    -- Check for user roles without valid users
    SELECT COUNT(*) INTO v_orphaned
    FROM user_roles ur
    WHERE NOT EXISTS (SELECT 1 FROM users u WHERE u.id = ur.user_id);

    IF v_orphaned > 0 THEN
        RAISE WARNING 'Found % user roles without valid users', v_orphaned;
    END IF;

    -- Check for team roles without valid teams
    SELECT COUNT(*) INTO v_invalid_roles
    FROM user_roles ur
    WHERE ur.scope_type = 'team'
      AND NOT EXISTS (SELECT 1 FROM teams t WHERE t.id = ur.scope_id);

    IF v_invalid_roles > 0 THEN
        RAISE WARNING 'Found % team roles without valid teams', v_invalid_roles;
    END IF;
END $$;

-- =============================================================================
-- MIGRATION METADATA
-- =============================================================================

-- Record this transformation
INSERT INTO __migrations_history (migration_id, migration_name, checksum, is_idempotent)
VALUES ('DT002', 'MigrateUsersToIdentityModel', MD5(pg_read_file('/scripts/data-transform/002_MigrateUserIdentity.sql')::text), true)
ON CONFLICT (migration_id) DO NOTHING;

-- =============================================================================
-- CLEANUP (Optional - run after verification)
-- =============================================================================

-- Note: Uncomment below only after full verification
-- DROP TABLE IF EXISTS users_legacy_backup;
