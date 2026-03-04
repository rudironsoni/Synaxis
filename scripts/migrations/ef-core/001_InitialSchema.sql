-- =============================================================================
-- Migration: 001_InitialSchema
-- Description: Initial database schema creation for Synaxis platform
-- Created: 2026-03-04
-- Idempotent: Yes
-- =============================================================================

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =============================================================================
-- CORE TABLES
-- =============================================================================

-- Organizations table
CREATE TABLE IF NOT EXISTS organizations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    slug VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT NOT NULL DEFAULT '',
    primary_region VARCHAR(50) NOT NULL DEFAULT 'us-east-1',
    tier VARCHAR(50) NOT NULL DEFAULT 'free',
    billing_currency VARCHAR(3) NOT NULL DEFAULT 'USD',
    credit_balance DECIMAL(18, 8) NOT NULL DEFAULT 0,
    credit_currency VARCHAR(3) NOT NULL DEFAULT 'USD',
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_verified BOOLEAN NOT NULL DEFAULT false,
    is_trial BOOLEAN NOT NULL DEFAULT false,
    require_sso BOOLEAN NOT NULL DEFAULT false,
    data_retention_days INTEGER NOT NULL DEFAULT 90,
    max_concurrent_requests INTEGER,
    max_keys_per_user INTEGER,
    max_teams INTEGER,
    max_users_per_team INTEGER,
    monthly_request_limit BIGINT,
    monthly_token_limit BIGINT,
    subscription_status VARCHAR(50) NOT NULL DEFAULT 'inactive',
    subscription_started_at TIMESTAMPTZ,
    subscription_expires_at TIMESTAMPTZ,
    trial_started_at TIMESTAMPTZ,
    trial_ends_at TIMESTAMPTZ,
    terms_accepted_at TIMESTAMPTZ,
    allowed_email_domains TEXT[] NOT NULL DEFAULT '{}',
    available_regions TEXT[] NOT NULL DEFAULT '{"us-east-1"}',
    privacy_consent JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_organization_slug_lowercase CHECK (slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$')
);

CREATE INDEX IF NOT EXISTS idx_organizations_slug ON organizations(slug);
CREATE INDEX IF NOT EXISTS idx_organizations_tier ON organizations(tier);
CREATE INDEX IF NOT EXISTS idx_organizations_primary_region ON organizations(primary_region);
CREATE INDEX IF NOT EXISTS idx_organizations_is_active ON organizations(is_active);

-- Users table
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    email VARCHAR(255) NOT NULL UNIQUE,
    email_verified_at TIMESTAMPTZ,
    password_hash TEXT NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    avatar_url TEXT,
    timezone VARCHAR(50) DEFAULT 'UTC',
    locale VARCHAR(10) DEFAULT 'en',
    role VARCHAR(50) NOT NULL DEFAULT 'Member',
    data_residency_region VARCHAR(50) DEFAULT 'us-east-1',
    created_in_region VARCHAR(50) DEFAULT 'us-east-1',
    cross_border_consent_given BOOLEAN NOT NULL DEFAULT false,
    cross_border_consent_date TIMESTAMPTZ,
    cross_border_consent_version VARCHAR(20),
    mfa_enabled BOOLEAN NOT NULL DEFAULT false,
    mfa_secret TEXT,
    mfa_backup_codes TEXT[],
    last_login_at TIMESTAMPTZ,
    failed_login_attempts INTEGER NOT NULL DEFAULT 0,
    locked_until TIMESTAMPTZ,
    password_expires_at TIMESTAMPTZ,
    failed_password_change_attempts INTEGER NOT NULL DEFAULT 0,
    password_change_locked_until TIMESTAMPTZ,
    password_changed_at TIMESTAMPTZ,
    must_change_password BOOLEAN NOT NULL DEFAULT false,
    is_active BOOLEAN NOT NULL DEFAULT true,
    privacy_consent JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_users_organization_id ON users(organization_id);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_data_residency_region ON users(data_residency_region);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON users(is_active);

-- Teams table
CREATE TABLE IF NOT EXISTS teams (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    slug VARCHAR(100) NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    monthly_budget DECIMAL(18, 2),
    budget_alert_threshold INTEGER CHECK (budget_alert_threshold >= 0 AND budget_alert_threshold <= 100),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_teams_org_slug UNIQUE (organization_id, slug),
    CONSTRAINT ck_team_monthly_budget_nonnegative CHECK (monthly_budget IS NULL OR monthly_budget >= 0)
);

CREATE INDEX IF NOT EXISTS idx_teams_organization_id ON teams(organization_id);
CREATE INDEX IF NOT EXISTS idx_teams_org_name ON teams(organization_id, name);

-- Team Memberships table
CREATE TABLE IF NOT EXISTS team_memberships (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    team_id UUID NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    role VARCHAR(50) NOT NULL DEFAULT 'Member',
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    invited_by UUID REFERENCES users(id) ON DELETE SET NULL,

    CONSTRAINT uq_team_memberships_user_team UNIQUE (user_id, team_id),
    CONSTRAINT ck_team_membership_role_valid CHECK (role IN ('OrgAdmin', 'TeamAdmin', 'Member', 'Viewer'))
);

CREATE INDEX IF NOT EXISTS idx_team_memberships_team_id ON team_memberships(team_id);
CREATE INDEX IF NOT EXISTS idx_team_memberships_org_user ON team_memberships(organization_id, user_id);

-- Virtual Keys table
CREATE TABLE IF NOT EXISTS virtual_keys (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key_hash VARCHAR(64) NOT NULL UNIQUE,
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    team_id UUID REFERENCES teams(id) ON DELETE CASCADE,
    created_by UUID REFERENCES users(id) ON DELETE RESTRICT,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_revoked BOOLEAN NOT NULL DEFAULT false,
    revoked_at TIMESTAMPTZ,
    revoked_reason TEXT,
    max_budget DECIMAL(18, 2),
    current_spend DECIMAL(18, 2) NOT NULL DEFAULT 0,
    rpm_limit INTEGER,
    tpm_limit INTEGER,
    expires_at TIMESTAMPTZ,
    user_region VARCHAR(50),
    metadata JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_virtual_key_max_budget_nonnegative CHECK (max_budget IS NULL OR max_budget >= 0),
    CONSTRAINT ck_virtual_key_current_spend_nonnegative CHECK (current_spend >= 0)
);

CREATE INDEX IF NOT EXISTS idx_virtual_keys_org_team ON virtual_keys(organization_id, team_id);
CREATE INDEX IF NOT EXISTS idx_virtual_keys_org_name ON virtual_keys(organization_id, name);
CREATE INDEX IF NOT EXISTS idx_virtual_keys_key_hash ON virtual_keys(key_hash);

-- =============================================================================
-- REQUESTS & AUDIT TABLES
-- =============================================================================

-- Requests table
CREATE TABLE IF NOT EXISTS requests (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    request_id UUID NOT NULL UNIQUE,
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    virtual_key_id UUID REFERENCES virtual_keys(id) ON DELETE SET NULL,
    team_id UUID REFERENCES teams(id) ON DELETE SET NULL,
    user_region VARCHAR(50) NOT NULL,
    processed_region VARCHAR(50) NOT NULL,
    stored_region VARCHAR(50) NOT NULL,
    cross_border_transfer BOOLEAN NOT NULL DEFAULT false,
    transfer_legal_basis TEXT,
    transfer_purpose TEXT,
    transfer_timestamp TIMESTAMPTZ,
    model VARCHAR(100) NOT NULL,
    provider VARCHAR(100) NOT NULL,
    input_tokens INTEGER NOT NULL DEFAULT 0,
    output_tokens INTEGER NOT NULL DEFAULT 0,
    cost DECIMAL(18, 8) NOT NULL DEFAULT 0,
    duration_ms INTEGER NOT NULL DEFAULT 0,
    queue_time_ms INTEGER NOT NULL DEFAULT 0,
    request_size_bytes INTEGER NOT NULL DEFAULT 0,
    response_size_bytes INTEGER NOT NULL DEFAULT 0,
    status_code INTEGER NOT NULL,
    client_ip_address INET NOT NULL,
    user_agent TEXT,
    request_headers JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_requests_org_created ON requests(organization_id, created_at);
CREATE INDEX IF NOT EXISTS idx_requests_virtual_key ON requests(virtual_key_id);
CREATE INDEX IF NOT EXISTS idx_requests_cross_border ON requests(cross_border_transfer);
CREATE INDEX IF NOT EXISTS idx_requests_created_at ON requests(created_at);

-- Audit Logs table
CREATE TABLE IF NOT EXISTS audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    event_type VARCHAR(100) NOT NULL,
    event_category VARCHAR(100) NOT NULL,
    action VARCHAR(255) NOT NULL,
    resource_type VARCHAR(100) NOT NULL,
    resource_id VARCHAR(255) NOT NULL,
    ip_address INET NOT NULL,
    user_agent VARCHAR(500) NOT NULL,
    region VARCHAR(50) NOT NULL,
    integrity_hash VARCHAR(128) NOT NULL,
    previous_hash VARCHAR(128) NOT NULL,
    metadata JSONB NOT NULL DEFAULT '{}',
    search_vector TSVECTOR,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_audit_logs_org_time ON audit_logs(organization_id, timestamp);
CREATE INDEX IF NOT EXISTS idx_audit_logs_event_type ON audit_logs(event_type);
CREATE INDEX IF NOT EXISTS idx_audit_logs_event_category ON audit_logs(event_category);
CREATE INDEX IF NOT EXISTS idx_audit_logs_timestamp ON audit_logs(timestamp);
CREATE INDEX IF NOT EXISTS idx_audit_logs_search_vector ON audit_logs USING GIN(search_vector);

-- Create trigger for search vector update
CREATE OR REPLACE FUNCTION update_audit_log_search_vector()
RETURNS TRIGGER AS $$
BEGIN
    NEW.search_vector :=
        setweight(to_tsvector('english', COALESCE(NEW.event_type, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.action, '')), 'B') ||
        setweight(to_tsvector('english', COALESCE(NEW.resource_type, '')), 'B') ||
        setweight(to_tsvector('english', COALESCE(NEW.resource_id, '')), 'C');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS audit_log_search_vector_update ON audit_logs;
CREATE TRIGGER audit_log_search_vector_update
    BEFORE INSERT OR UPDATE ON audit_logs
    FOR EACH ROW
    EXECUTE FUNCTION update_audit_log_search_vector();

-- =============================================================================
-- BILLING TABLES
-- =============================================================================

-- Credit Transactions table
CREATE TABLE IF NOT EXISTS credit_transactions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    transaction_type VARCHAR(50) NOT NULL,
    amount_usd DECIMAL(18, 8) NOT NULL,
    balance_before_usd DECIMAL(18, 8) NOT NULL,
    balance_after_usd DECIMAL(18, 8) NOT NULL,
    description VARCHAR(500) NOT NULL,
    reference_id UUID,
    initiated_by UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_credit_transactions_org ON credit_transactions(organization_id);
CREATE INDEX IF NOT EXISTS idx_credit_transactions_type ON credit_transactions(transaction_type);
CREATE INDEX IF NOT EXISTS idx_credit_transactions_org_created ON credit_transactions(organization_id, created_at);

-- Invoices table
CREATE TABLE IF NOT EXISTS invoices (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    invoice_number VARCHAR(50) NOT NULL UNIQUE,
    status VARCHAR(50) NOT NULL,
    period_start TIMESTAMPTZ NOT NULL,
    period_end TIMESTAMPTZ NOT NULL,
    total_amount_usd DECIMAL(18, 8) NOT NULL,
    total_amount_billing_currency DECIMAL(18, 8) NOT NULL,
    billing_currency VARCHAR(3) NOT NULL,
    exchange_rate DECIMAL(18, 8) NOT NULL DEFAULT 1,
    due_date TIMESTAMPTZ,
    paid_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_invoices_org ON invoices(organization_id);
CREATE INDEX IF NOT EXISTS idx_invoices_status ON invoices(status);
CREATE INDEX IF NOT EXISTS idx_invoices_org_period ON invoices(organization_id, period_start, period_end);

-- Spend Logs table
CREATE TABLE IF NOT EXISTS spend_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    team_id UUID REFERENCES teams(id) ON DELETE SET NULL,
    virtual_key_id UUID REFERENCES virtual_keys(id) ON DELETE SET NULL,
    request_id UUID REFERENCES requests(id) ON DELETE SET NULL,
    amount_usd DECIMAL(18, 8) NOT NULL,
    model VARCHAR(100) NOT NULL,
    provider VARCHAR(100) NOT NULL,
    tokens INTEGER NOT NULL DEFAULT 0,
    region VARCHAR(50) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_spend_logs_org ON spend_logs(organization_id);
CREATE INDEX IF NOT EXISTS idx_spend_logs_team ON spend_logs(team_id);
CREATE INDEX IF NOT EXISTS idx_spend_logs_created ON spend_logs(created_at);

-- Subscription Plans table
CREATE TABLE IF NOT EXISTS subscription_plans (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    slug VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    monthly_price_usd DECIMAL(18, 2) NOT NULL,
    yearly_price_usd DECIMAL(18, 2) NOT NULL,
    limits_config JSONB NOT NULL DEFAULT '{}',
    features JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_subscription_plans_slug ON subscription_plans(slug);
CREATE INDEX IF NOT EXISTS idx_subscription_plans_active ON subscription_plans(is_active);

-- =============================================================================
-- AUTHENTICATION & SECURITY TABLES
-- =============================================================================

-- Refresh Tokens table
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash TEXT NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ,
    replaced_by_token_hash TEXT,
    is_revoked BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_expires ON refresh_tokens(expires_at);

-- JWT Blacklist table
CREATE TABLE IF NOT EXISTS jwt_blacklists (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_id TEXT NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_jwt_blacklists_user ON jwt_blacklists(user_id);
CREATE INDEX IF NOT EXISTS idx_jwt_blacklists_expires ON jwt_blacklists(expires_at);

-- Password History table
CREATE TABLE IF NOT EXISTS password_histories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    password_hash TEXT NOT NULL,
    set_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_password_histories_user ON password_histories(user_id);
CREATE INDEX IF NOT EXISTS idx_password_histories_user_set_at ON password_histories(user_id, set_at);

-- Password Policies table
CREATE TABLE IF NOT EXISTS password_policies (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL UNIQUE REFERENCES organizations(id) ON DELETE CASCADE,
    min_length INTEGER NOT NULL DEFAULT 8,
    require_uppercase BOOLEAN NOT NULL DEFAULT true,
    require_lowercase BOOLEAN NOT NULL DEFAULT true,
    require_numbers BOOLEAN NOT NULL DEFAULT true,
    require_special_characters BOOLEAN NOT NULL DEFAULT true,
    block_common_passwords BOOLEAN NOT NULL DEFAULT true,
    block_user_info_in_password BOOLEAN NOT NULL DEFAULT true,
    password_history_count INTEGER NOT NULL DEFAULT 5,
    password_expiration_days INTEGER,
    password_expiration_warning_days INTEGER DEFAULT 7,
    lockout_duration_minutes INTEGER NOT NULL DEFAULT 30,
    max_failed_change_attempts INTEGER NOT NULL DEFAULT 5,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Organization API Keys table
CREATE TABLE IF NOT EXISTS organization_api_keys (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    key_hash VARCHAR(64) NOT NULL UNIQUE,
    key_prefix VARCHAR(8) NOT NULL,
    name VARCHAR(255) NOT NULL,
    permissions JSONB NOT NULL DEFAULT '{}',
    is_active BOOLEAN NOT NULL DEFAULT true,
    expires_at TIMESTAMPTZ,
    revoked_at TIMESTAMPTZ,
    revoked_reason VARCHAR(500) NOT NULL DEFAULT '',
    last_used_at TIMESTAMPTZ,
    total_requests BIGINT DEFAULT 0,
    error_count BIGINT DEFAULT 0,
    created_by UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_org_api_keys_org ON organization_api_keys(organization_id);
CREATE INDEX IF NOT EXISTS idx_org_api_keys_hash ON organization_api_keys(key_hash);

-- =============================================================================
-- UTILITY TABLES
-- =============================================================================

-- Backup Config table
CREATE TABLE IF NOT EXISTS organization_backup_config (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    strategy VARCHAR(50) NOT NULL DEFAULT 'incremental',
    frequency VARCHAR(50) NOT NULL DEFAULT 'daily',
    schedule_hour INTEGER NOT NULL DEFAULT 2,
    enable_encryption BOOLEAN NOT NULL DEFAULT true,
    encryption_key_id TEXT,
    retention_days INTEGER NOT NULL DEFAULT 30,
    enable_postgres_backup BOOLEAN NOT NULL DEFAULT true,
    enable_redis_backup BOOLEAN NOT NULL DEFAULT true,
    enable_qdrant_backup BOOLEAN NOT NULL DEFAULT true,
    target_regions TEXT[] NOT NULL DEFAULT '{}',
    is_active BOOLEAN NOT NULL DEFAULT true,
    last_backup_at TIMESTAMPTZ,
    last_backup_status TEXT DEFAULT 'never',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_backup_config_org ON organization_backup_config(organization_id);
CREATE INDEX IF NOT EXISTS idx_backup_config_org_active ON organization_backup_config(organization_id, is_active);

-- Invitations table
CREATE TABLE IF NOT EXISTS invitations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    team_id UUID REFERENCES teams(id) ON DELETE CASCADE,
    email VARCHAR(255) NOT NULL,
    role VARCHAR(50) NOT NULL,
    token VARCHAR(128) NOT NULL UNIQUE,
    invited_by UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    accepted_at TIMESTAMPTZ,
    accepted_by UUID REFERENCES users(id) ON DELETE SET NULL,
    declined_at TIMESTAMPTZ,
    declined_by UUID REFERENCES users(id) ON DELETE SET NULL,
    cancelled_at TIMESTAMPTZ,
    cancelled_by UUID REFERENCES users(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_invitations_org_status ON invitations(organization_id, status);
CREATE INDEX IF NOT EXISTS idx_invitations_team_email ON invitations(team_id, email, status);

-- Collections table
CREATE TABLE IF NOT EXISTS collections (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    team_id UUID REFERENCES teams(id) ON DELETE SET NULL,
    slug VARCHAR(100) NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT NOT NULL DEFAULT '',
    type VARCHAR(50) NOT NULL,
    visibility VARCHAR(20) NOT NULL DEFAULT 'private',
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB NOT NULL DEFAULT '{}',
    tags JSONB NOT NULL DEFAULT '[]',
    created_by UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_collections_org_slug UNIQUE (organization_id, slug)
);

CREATE INDEX IF NOT EXISTS idx_collections_org ON collections(organization_id);
CREATE INDEX IF NOT EXISTS idx_collections_created_by ON collections(created_by);

-- Collection Memberships table
CREATE TABLE IF NOT EXISTS collection_memberships (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    collection_id UUID NOT NULL REFERENCES collections(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    role VARCHAR(50) NOT NULL DEFAULT 'viewer',
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    added_by UUID REFERENCES users(id) ON DELETE SET NULL,

    CONSTRAINT uq_collection_memberships_user_collection UNIQUE (user_id, collection_id)
);

CREATE INDEX IF NOT EXISTS idx_collection_memberships_collection ON collection_memberships(collection_id);
CREATE INDEX IF NOT EXISTS idx_collection_memberships_org_user ON collection_memberships(organization_id, user_id);

-- Conversations table
CREATE TABLE IF NOT EXISTS conversations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(200) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_conversations_org ON conversations(organization_id);
CREATE INDEX IF NOT EXISTS idx_conversations_user ON conversations(user_id);
CREATE INDEX IF NOT EXISTS idx_conversations_created ON conversations(created_at);

-- Conversation Turns table
CREATE TABLE IF NOT EXISTS conversation_turns (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    conversation_id UUID NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    role VARCHAR(50) NOT NULL,
    content TEXT NOT NULL,
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_conversation_turns_conversation ON conversation_turns(conversation_id);
CREATE INDEX IF NOT EXISTS idx_conversation_turns_created ON conversation_turns(created_at);

-- =============================================================================
-- MIGRATION METADATA
-- =============================================================================

-- Track applied migrations
CREATE TABLE IF NOT EXISTS __migrations_history (
    migration_id VARCHAR(255) PRIMARY KEY,
    migration_name VARCHAR(255) NOT NULL,
    applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    applied_by TEXT NOT NULL DEFAULT CURRENT_USER,
    execution_time_ms INTEGER,
    checksum VARCHAR(64),
    is_idempotent BOOLEAN NOT NULL DEFAULT true
);

-- Record this migration
INSERT INTO __migrations_history (migration_id, migration_name, checksum, is_idempotent)
VALUES ('001', 'InitialSchema', MD5(pg_read_file('/scripts/migrations/ef-core/001_InitialSchema.sql')::text), true)
ON CONFLICT (migration_id) DO NOTHING;

-- =============================================================================
-- ROW-LEVEL SECURITY POLICIES (Optional - for multi-tenant security)
-- =============================================================================

-- Enable RLS on tables
ALTER TABLE organizations ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE teams ENABLE ROW LEVEL SECURITY;
ALTER TABLE team_memberships ENABLE ROW LEVEL SECURITY;

-- Create policy function
CREATE OR REPLACE FUNCTION get_current_organization_id()
RETURNS UUID AS $$
BEGIN
    RETURN NULLIF(current_setting('app.current_organization_id', true), '')::UUID;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Note: Actual RLS policies would be added here based on application requirements
-- For now, we just enable RLS - policies should be added based on specific security needs

-- =============================================================================
-- COMMENTS
-- =============================================================================

COMMENT ON TABLE organizations IS 'Multi-tenant organizations';
COMMENT ON TABLE users IS 'User accounts within organizations';
COMMENT ON TABLE teams IS 'Teams within organizations for resource grouping';
COMMENT ON TABLE virtual_keys IS 'API keys for authentication';
COMMENT ON TABLE requests IS 'LLM inference requests';
COMMENT ON TABLE audit_logs IS 'Audit trail for compliance';
COMMENT ON TABLE credit_transactions IS 'Credit balance changes';
COMMENT ON TABLE invoices IS 'Billing invoices';
