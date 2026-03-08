-- Multi-Tenant Foundation Migration
-- Created: 2025-02-05
-- Description: Initial schema for multi-region, multi-tenant Synaxis platform

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================
-- ORGANIZATIONS (Tenant Root)
-- ============================================
CREATE TABLE organizations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    slug VARCHAR(100) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    
    -- Regional Configuration
    primary_region VARCHAR(50) NOT NULL,
    available_regions TEXT[] DEFAULT ARRAY['eu-west-1', 'us-east-1', 'sa-east-1'],
    
    -- Subscription & Billing
    tier VARCHAR(50) NOT NULL DEFAULT 'free' CHECK (tier IN ('free', 'pro', 'enterprise')),
    billing_currency VARCHAR(3) NOT NULL DEFAULT 'USD' CHECK (billing_currency IN ('USD', 'EUR', 'BRL', 'GBP')),
    credit_balance DECIMAL(12,4) DEFAULT 0.00,
    credit_currency VARCHAR(3) DEFAULT 'USD',
    
    -- Subscription State
    subscription_status VARCHAR(50) DEFAULT 'active' CHECK (subscription_status IN ('active', 'paused', 'cancelled', 'expired')),
    subscription_started_at TIMESTAMPTZ DEFAULT NOW(),
    subscription_expires_at TIMESTAMPTZ,
    
    -- Trial
    is_trial BOOLEAN DEFAULT false,
    trial_started_at TIMESTAMPTZ,
    trial_ends_at TIMESTAMPTZ,
    
    -- Resource Quotas (NULL = use tier defaults)
    max_teams INT,
    max_users_per_team INT,
    max_keys_per_user INT,
    max_concurrent_requests INT,
    monthly_request_limit BIGINT,
    monthly_token_limit BIGINT,
    
    -- Compliance
    data_retention_days INT DEFAULT 30,
    require_sso BOOLEAN DEFAULT false,
    allowed_email_domains TEXT[],
    
    -- GDPR/LGPD Consent
    privacy_consent JSONB DEFAULT '{}',
    terms_accepted_at TIMESTAMPTZ,
    
    -- Status
    is_active BOOLEAN DEFAULT true,
    is_verified BOOLEAN DEFAULT false,
    
    -- Timestamps
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT valid_primary_region CHECK (primary_region IN ('eu-west-1', 'us-east-1', 'sa-east-1'))
);

CREATE INDEX idx_organizations_slug ON organizations(slug);
CREATE INDEX idx_organizations_tier ON organizations(tier);
CREATE INDEX idx_organizations_region ON organizations(primary_region);

-- ============================================
-- TEAMS
-- ============================================
CREATE TABLE teams (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    slug VARCHAR(100) NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    
    -- Team Configuration
    is_active BOOLEAN DEFAULT true,
    
    -- Team Budget (cascades to all team keys)
    monthly_budget DECIMAL(12,4),
    budget_alert_threshold DECIMAL(5,2) DEFAULT 80.00,
    
    -- Model Access (NULL = inherit from org)
    allowed_models TEXT[],
    blocked_models TEXT[],
    
    -- Timestamps
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Unique constraint
    UNIQUE(organization_id, slug)
);

CREATE INDEX idx_teams_organization ON teams(organization_id);

-- ============================================
-- USERS
-- ============================================
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Authentication
    email VARCHAR(255) UNIQUE NOT NULL,
    email_verified_at TIMESTAMPTZ,
    password_hash VARCHAR(255) NOT NULL,
    
    -- Multi-Geo Fields (CRITICAL)
    data_residency_region VARCHAR(50) NOT NULL,
    created_in_region VARCHAR(50) NOT NULL,
    
    -- Profile
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    avatar_url TEXT,
    timezone VARCHAR(50) DEFAULT 'UTC',
    locale VARCHAR(10) DEFAULT 'en-US',
    
    -- Role
    role VARCHAR(50) DEFAULT 'member' CHECK (role IN ('owner', 'admin', 'member', 'viewer')),
    
    -- Compliance Consent Tracking
    privacy_consent JSONB DEFAULT '{}',
    -- Example: {"gdpr": true, "lgpd": true, "date": "2025-02-05", "version": "1.0"}
    
    cross_border_consent_given BOOLEAN DEFAULT false,
    cross_border_consent_date TIMESTAMPTZ,
    cross_border_consent_version VARCHAR(20),
    
    -- Security
    mfa_enabled BOOLEAN DEFAULT false,
    mfa_secret VARCHAR(255),
    last_login_at TIMESTAMPTZ,
    failed_login_attempts INT DEFAULT 0,
    locked_until TIMESTAMPTZ,
    
    -- Status
    is_active BOOLEAN DEFAULT true,
    
    -- Timestamps
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT valid_data_region CHECK (data_residency_region IN ('eu-west-1', 'us-east-1', 'sa-east-1')),
    CONSTRAINT valid_created_region CHECK (created_in_region IN ('eu-west-1', 'us-east-1', 'sa-east-1'))
);

CREATE INDEX idx_users_organization ON users(organization_id);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_region ON users(data_residency_region);

-- ============================================
-- TEAM MEMBERSHIPS
-- ============================================
CREATE TABLE team_memberships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    team_id UUID NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    role VARCHAR(50) NOT NULL DEFAULT 'member' CHECK (role IN ('admin', 'member')),
    
    joined_at TIMESTAMPTZ DEFAULT NOW(),
    invited_by UUID REFERENCES users(id),
    
    UNIQUE(user_id, team_id)
);

CREATE INDEX idx_team_memberships_team ON team_memberships(team_id);
CREATE INDEX idx_team_memberships_user ON team_memberships(user_id);

-- ============================================
-- VIRTUAL KEYS (API Keys)
-- ============================================
CREATE TABLE virtual_keys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    key_hash VARCHAR(255) UNIQUE NOT NULL,
    
    -- Tenant Scoping
    organization_id UUID NOT NULL REFERENCES organizations(id),
    team_id UUID NOT NULL REFERENCES teams(id),
    created_by UUID NOT NULL REFERENCES users(id),
    
    -- Key Configuration
    name VARCHAR(255),
    description TEXT,
    is_active BOOLEAN DEFAULT true,
    is_revoked BOOLEAN DEFAULT false,
    revoked_at TIMESTAMPTZ,
    revoked_reason TEXT,
    
    -- Budget (inherits from team/org if NULL)
    max_budget DECIMAL(12,4),
    current_spend DECIMAL(12,4) DEFAULT 0.00,
    
    -- Rate Limits
    rpm_limit INT,
    tpm_limit INT,
    
    -- Model Access (inherits from team if NULL)
    allowed_models TEXT[],
    blocked_models TEXT[],
    
    -- Expiry
    expires_at TIMESTAMPTZ,
    
    -- Metadata
    tags TEXT[],
    metadata JSONB DEFAULT '{}',
    
    -- Region for partitioning
    user_region VARCHAR(50) NOT NULL,
    
    -- Timestamps
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Indexes
    INDEX idx_virtual_keys_org_team (organization_id, team_id),
    INDEX idx_virtual_keys_created_by (created_by),
    INDEX idx_virtual_keys_region (user_region)
);

-- ============================================
-- REQUESTS (Partitioned by Region)
-- ============================================
CREATE TABLE requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id UUID UNIQUE NOT NULL,
    
    -- Tenant Context
    organization_id UUID NOT NULL,
    user_id UUID,
    virtual_key_id UUID,
    team_id UUID,
    
    -- Region Tracking (CRITICAL for compliance)
    user_region VARCHAR(50) NOT NULL,
    processed_region VARCHAR(50) NOT NULL,
    stored_region VARCHAR(50) NOT NULL,
    
    -- Cross-Border Transfer Tracking
    cross_border_transfer BOOLEAN DEFAULT false,
    transfer_legal_basis VARCHAR(50), -- 'SCC', 'consent', 'adequacy', 'none'
    transfer_purpose VARCHAR(100),
    transfer_timestamp TIMESTAMPTZ,
    
    -- Request Data
    model VARCHAR(100) NOT NULL,
    provider VARCHAR(100),
    
    -- Token Usage
    input_tokens INT DEFAULT 0,
    output_tokens INT DEFAULT 0,
    total_tokens INT GENERATED ALWAYS AS (input_tokens + output_tokens) STORED,
    
    -- Cost (in USD)
    cost DECIMAL(12,6) DEFAULT 0.000000,
    
    -- Performance
    duration_ms INT,
    queue_time_ms INT,
    
    -- Request/Response Metadata
    request_size_bytes INT,
    response_size_bytes INT,
    status_code INT,
    
    -- Client Info
    client_ip INET,
    user_agent TEXT,
    request_headers JSONB,
    
    -- Timestamps
    created_at TIMESTAMPTZ DEFAULT NOW(),
    completed_at TIMESTAMPTZ,
    
    -- Partition by region
    CONSTRAINT valid_stored_region CHECK (stored_region IN ('eu-west-1', 'us-east-1', 'sa-east-1'))
) PARTITION BY LIST (stored_region);

-- Create partitions
CREATE TABLE requests_eu PARTITION OF requests FOR VALUES IN ('eu-west-1');
CREATE TABLE requests_us PARTITION OF requests FOR VALUES IN ('us-east-1');
CREATE TABLE requests_br PARTITION OF requests FOR VALUES IN ('sa-east-1');

-- Indexes on partitions
CREATE INDEX idx_requests_eu_org ON requests_eu(organization_id, created_at DESC);
CREATE INDEX idx_requests_us_org ON requests_us(organization_id, created_at DESC);
CREATE INDEX idx_requests_br_org ON requests_br(organization_id, created_at DESC);

-- ============================================
-- SPEND LOGS (Partitioned by Region)
-- ============================================
CREATE TABLE spend_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL,
    user_id UUID,
    virtual_key_id UUID,
    team_id UUID,
    
    -- Region
    user_region VARCHAR(50) NOT NULL,
    
    -- Spend Details
    request_id UUID,
    model VARCHAR(100),
    input_tokens INT DEFAULT 0,
    output_tokens INT DEFAULT 0,
    cost DECIMAL(12,6) DEFAULT 0.000000,
    
    -- Metadata
    created_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Partition by region
    CONSTRAINT valid_spend_region CHECK (user_region IN ('eu-west-1', 'us-east-1', 'sa-east-1'))
) PARTITION BY LIST (user_region);

CREATE TABLE spend_logs_eu PARTITION OF spend_logs FOR VALUES IN ('eu-west-1');
CREATE TABLE spend_logs_us PARTITION OF spend_logs FOR VALUES IN ('us-east-1');
CREATE TABLE spend_logs_br PARTITION OF spend_logs FOR VALUES IN ('sa-east-1');

CREATE INDEX idx_spend_eu_org ON spend_logs_eu(organization_id, created_at DESC);
CREATE INDEX idx_spend_us_org ON spend_logs_us(organization_id, created_at DESC);
CREATE INDEX idx_spend_br_org ON spend_logs_br(organization_id, created_at DESC);

-- ============================================
-- USAGE QUOTAS
-- ============================================
CREATE TABLE usage_quotas (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL,
    user_id UUID,
    virtual_key_id UUID,
    
    -- Metric Definition
    metric_type VARCHAR(50) NOT NULL CHECK (metric_type IN ('requests', 'tokens')),
    time_granularity VARCHAR(50) NOT NULL CHECK (time_granularity IN ('minute', 'hour', 'day', 'week', 'month')),
    
    -- Window Configuration
    window_type VARCHAR(50) NOT NULL CHECK (window_type IN ('fixed', 'sliding')),
    window_start TIMESTAMPTZ NOT NULL,
    window_end TIMESTAMPTZ NOT NULL,
    
    -- Usage
    current_value BIGINT DEFAULT 0,
    limit_value BIGINT NOT NULL,
    is_exceeded BOOLEAN DEFAULT false,
    exceeded_at TIMESTAMPTZ,
    
    -- Region
    region VARCHAR(50) NOT NULL,
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    
    UNIQUE(organization_id, user_id, virtual_key_id, metric_type, time_granularity, window_start)
);

CREATE INDEX idx_quotas_org ON usage_quotas(organization_id, metric_type, time_granularity, window_start);

-- ============================================
-- AUDIT LOGS (Immutable)
-- ============================================
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- Context
    organization_id UUID,
    user_id UUID,
    
    -- Action
    action VARCHAR(100) NOT NULL, -- 'create', 'update', 'delete', 'view', 'export', etc.
    resource_type VARCHAR(100) NOT NULL, -- 'organization', 'user', 'key', 'request', etc.
    resource_id UUID,
    
    -- Details
    description TEXT,
    old_values JSONB,
    new_values JSONB,
    
    -- Security
    client_ip INET,
    user_agent TEXT,
    session_id VARCHAR(255),
    
    -- Region
    region VARCHAR(50) NOT NULL,
    
    -- Timestamps
    created_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Immutable (no updates/deletes allowed)
    CONSTRAINT audit_logs_immutable CHECK (false) -- Enforced via trigger
);

-- Prevent updates/deletes on audit_logs
CREATE OR REPLACE FUNCTION prevent_audit_modification()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'Audit logs are immutable and cannot be modified';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER audit_logs_no_update
    BEFORE UPDATE OR DELETE ON audit_logs
    FOR EACH ROW
    EXECUTE FUNCTION prevent_audit_modification();

CREATE INDEX idx_audit_org ON audit_logs(organization_id, created_at DESC);

-- ============================================
-- CROSS-BORDER TRANSFERS
-- ============================================
CREATE TABLE cross_border_transfers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- Transfer Details
    organization_id UUID NOT NULL,
    user_id UUID,
    
    from_region VARCHAR(50) NOT NULL,
    to_region VARCHAR(50) NOT NULL,
    
    -- Legal Basis
    legal_basis VARCHAR(50) NOT NULL, -- 'SCC', 'consent', 'adequacy', 'contract'
    purpose VARCHAR(255) NOT NULL,
    
    -- Data Categories
    data_categories TEXT[], -- ['personal_data', 'usage_data', etc.]
    
    -- Safeguards
    encryption_used BOOLEAN DEFAULT true,
    encryption_method VARCHAR(100), -- 'AES-256-GCM'
    
    -- User Consent (if applicable)
    user_consent_obtained BOOLEAN DEFAULT false,
    user_consent_date TIMESTAMPTZ,
    
    -- Timestamps
    transfer_started_at TIMESTAMPTZ DEFAULT NOW(),
    transfer_completed_at TIMESTAMPTZ,
    
    -- Audit
    initiated_by UUID REFERENCES users(id),
    ip_address INET
);

CREATE INDEX idx_transfers_org ON cross_border_transfers(organization_id, transfer_started_at DESC);

-- ============================================
-- BACKUP CONFIGURATION
-- ============================================
CREATE TABLE organization_backup_config (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    
    -- Strategy
    strategy VARCHAR(50) NOT NULL DEFAULT 'regional_only' CHECK (strategy IN ('regional_only', 'cross_region', 'multi_cloud')),
    
    -- Regional Backup
    regional_backup_enabled BOOLEAN DEFAULT true,
    regional_retention_days INT DEFAULT 30,
    regional_backup_frequency VARCHAR(20) DEFAULT 'daily' CHECK (regional_backup_frequency IN ('hourly', 'daily', 'weekly')),
    regional_backup_time TIME DEFAULT '02:00:00',
    
    -- Cross-Region Backup
    cross_region_backup_enabled BOOLEAN DEFAULT false,
    cross_region_target_region VARCHAR(50),
    cross_region_retention_days INT DEFAULT 90,
    
    -- Encryption
    encryption_key_id VARCHAR(255),
    
    -- GDPR/LGPD Specific
    include_pii_in_backups BOOLEAN DEFAULT true,
    anonymize_after_days INT,
    
    -- Status
    last_backup_at TIMESTAMPTZ,
    last_backup_status VARCHAR(50),
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- SUBSCRIPTION PLANS (Templates)
-- ============================================
CREATE TABLE subscription_plans (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    slug VARCHAR(100) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    is_active BOOLEAN DEFAULT true,
    
    -- Pricing
    monthly_price_usd DECIMAL(10,2),
    yearly_price_usd DECIMAL(10,2),
    
    -- Limits Configuration (JSON for flexibility)
    limits_config JSONB NOT NULL,
    
    -- Features
    features JSONB DEFAULT '{}',
    -- Example: {"multi_geo": true, "sso": false, "audit_logs": true}
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Insert default plans
INSERT INTO subscription_plans (slug, name, description, limits_config, features) VALUES
('free', 'Free', 'Free tier for experimentation', '{
    "max_teams": 1,
    "max_users_per_team": 3,
    "max_keys_per_user": 2,
    "max_concurrent_requests": 10,
    "monthly_request_limit": 10000,
    "monthly_token_limit": 100000,
    "data_retention_days": 30,
    "allowed_regions": ["us-east-1"],
    "supports_custom_models": false,
    "supports_audit_logs": false,
    "supports_sso": false
}'::jsonb, '{"multi_geo": false, "custom_backup": false, "sso": false, "audit_logs": false}'::jsonb),

('pro', 'Pro', 'Professional tier for production workloads', '{
    "max_teams": 5,
    "max_users_per_team": 20,
    "max_keys_per_user": 10,
    "max_concurrent_requests": 100,
    "monthly_request_limit": 100000,
    "monthly_token_limit": 10000000,
    "data_retention_days": 90,
    "allowed_regions": ["eu-west-1", "us-east-1", "sa-east-1"],
    "supports_custom_models": true,
    "supports_audit_logs": true,
    "supports_sso": false
}'::jsonb, '{"multi_geo": true, "custom_backup": true, "sso": false, "audit_logs": true}'::jsonb),

('enterprise', 'Enterprise', 'Enterprise tier with custom limits', '{
    "max_teams": null,
    "max_users_per_team": null,
    "max_keys_per_user": null,
    "max_concurrent_requests": 1000,
    "monthly_request_limit": null,
    "monthly_token_limit": null,
    "data_retention_days": 365,
    "allowed_regions": ["eu-west-1", "us-east-1", "sa-east-1"],
    "supports_custom_models": true,
    "supports_audit_logs": true,
    "supports_sso": true
}'::jsonb, '{"multi_geo": true, "custom_backup": true, "sso": true, "audit_logs": true}'::jsonb);

-- ============================================
-- ROW LEVEL SECURITY (RLS) POLICIES
-- ============================================

-- Enable RLS on tenant tables
ALTER TABLE organizations ENABLE ROW LEVEL SECURITY;
ALTER TABLE teams ENABLE ROW LEVEL SECURITY;
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE team_memberships ENABLE ROW LEVEL SECURITY;
ALTER TABLE virtual_keys ENABLE ROW LEVEL SECURITY;
ALTER TABLE requests ENABLE ROW LEVEL SECURITY;
ALTER TABLE spend_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE usage_quotas ENABLE ROW LEVEL SECURITY;

-- Create policy function
CREATE OR REPLACE FUNCTION get_current_organization_id()
RETURNS UUID AS $$
BEGIN
    RETURN NULLIF(current_setting('app.current_organization_id', true), '')::UUID;
END;
$$ LANGUAGE plpgsql;

-- Organizations: Users can see their own org
CREATE POLICY tenant_isolation_organizations ON organizations
    FOR ALL
    USING (id = get_current_organization_id() OR 
           current_user = 'synaxis_superadmin');

-- Teams: Scoped to organization
CREATE POLICY tenant_isolation_teams ON teams
    FOR ALL
    USING (organization_id = get_current_organization_id() OR 
           current_user = 'synaxis_superadmin');

-- Users: Scoped to organization
CREATE POLICY tenant_isolation_users ON users
    FOR ALL
    USING (organization_id = get_current_organization_id() OR 
           current_user = 'synaxis_superadmin');

-- Virtual Keys: Scoped to organization
CREATE POLICY tenant_isolation_keys ON virtual_keys
    FOR ALL
    USING (organization_id = get_current_organization_id() OR 
           current_user = 'synaxis_superadmin');

-- ============================================
-- TRIGGERS FOR UPDATED_AT
-- ============================================
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_organizations_updated_at BEFORE UPDATE ON organizations
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_teams_updated_at BEFORE UPDATE ON teams
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_virtual_keys_updated_at BEFORE UPDATE ON virtual_keys
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- MATERIALIZED VIEWS FOR ANALYTICS
-- ============================================

-- Daily spend by organization
CREATE MATERIALIZED VIEW daily_spend_by_org AS
SELECT 
    organization_id,
    user_region,
    DATE(created_at) as date,
    model,
    SUM(cost) as total_cost,
    SUM(input_tokens) as total_input_tokens,
    SUM(output_tokens) as total_output_tokens,
    COUNT(*) as request_count
FROM spend_logs
GROUP BY organization_id, user_region, DATE(created_at), model;

CREATE INDEX idx_daily_spend_org ON daily_spend_by_org(organization_id, date);

-- Function to refresh materialized view
CREATE OR REPLACE FUNCTION refresh_daily_spend()
RETURNS void AS $$
BEGIN
    REFRESH MATERIALIZED VIEW CONCURRENTLY daily_spend_by_org;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- COMMENTS FOR DOCUMENTATION
-- ============================================

COMMENT ON TABLE organizations IS 'Root tenant entity. All data is scoped to an organization.';
COMMENT ON TABLE users IS 'User accounts with data residency tracking for GDPR/LGPD compliance';
COMMENT ON TABLE requests IS 'API requests partitioned by stored_region for data sovereignty';
COMMENT ON COLUMN users.data_residency_region IS 'Region where user data must be stored (GDPR/LGPD compliance)';
COMMENT ON COLUMN requests.cross_border_transfer IS 'True if request was processed in different region than user data residency';

-- Migration complete
SELECT 'Multi-tenant foundation schema created successfully' as status;
