-- Migration: 003_ApiManagementTables
-- Description: API keys, rate limits, and API management tables
-- Idempotent: Yes
-- Created: 2026-03-04

-- ============================================================================
-- API Keys Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS api_keys (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key_hash VARCHAR(512) NOT NULL UNIQUE,
    key_prefix VARCHAR(16) NOT NULL,
    key_suffix VARCHAR(8) NOT NULL,
    organization_id UUID NOT NULL,
    team_id UUID,
    user_id UUID,
    name VARCHAR(256) NOT NULL,
    description TEXT,
    permissions JSONB DEFAULT '[]',
    scopes TEXT[] DEFAULT '{}',
    allowed_origins TEXT[] DEFAULT '{}',
    allowed_ips TEXT[] DEFAULT '{}',
    is_active BOOLEAN DEFAULT TRUE,
    is_revoked BOOLEAN DEFAULT FALSE,
    revoked_at TIMESTAMP WITH TIME ZONE,
    revoked_reason TEXT,
    revoked_by UUID,
    expires_at TIMESTAMP WITH TIME ZONE,
    last_used_at TIMESTAMP WITH TIME ZONE,
    last_used_ip VARCHAR(45),
    last_used_endpoint VARCHAR(512),
    total_requests BIGINT DEFAULT 0,
    total_tokens BIGINT DEFAULT 0,
    total_cost DECIMAL(18,8) DEFAULT 0.0,
    error_count BIGINT DEFAULT 0,
    rate_limit_tier VARCHAR(50) DEFAULT 'standard',
    metadata JSONB DEFAULT '{}',
    created_by UUID,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    deleted_at TIMESTAMP WITH TIME ZONE
);

-- API Keys indexes
CREATE INDEX IF NOT EXISTS idx_api_keys_org_id ON api_keys(organization_id);
CREATE INDEX IF NOT EXISTS idx_api_keys_team_id ON api_keys(team_id);
CREATE INDEX IF NOT EXISTS idx_api_keys_user_id ON api_keys(user_id);
CREATE INDEX IF NOT EXISTS idx_api_keys_active ON api_keys(is_active);
CREATE INDEX IF NOT EXISTS idx_api_keys_revoked ON api_keys(is_revoked);
CREATE INDEX IF NOT EXISTS idx_api_keys_expires ON api_keys(expires_at);
CREATE INDEX IF NOT EXISTS idx_api_keys_last_used ON api_keys(last_used_at);
CREATE INDEX IF NOT EXISTS idx_api_keys_rate_tier ON api_keys(rate_limit_tier);
CREATE INDEX IF NOT EXISTS idx_api_keys_created_at ON api_keys(created_at);
CREATE UNIQUE INDEX IF NOT EXISTS idx_api_keys_hash ON api_keys(key_hash);

-- API Keys GIN indexes
CREATE INDEX IF NOT EXISTS idx_api_keys_permissions_gin ON api_keys USING GIN(permissions);
CREATE INDEX IF NOT EXISTS idx_api_keys_metadata_gin ON api_keys USING GIN(metadata);

-- API Keys constraints
ALTER TABLE api_keys
    ADD CONSTRAINT IF NOT EXISTS chk_api_keys_total_requests_non_negative CHECK (total_requests >= 0),
    ADD CONSTRAINT IF NOT EXISTS chk_api_keys_total_tokens_non_negative CHECK (total_tokens >= 0),
    ADD CONSTRAINT IF NOT EXISTS chk_api_keys_total_cost_non_negative CHECK (total_cost >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_api_keys_error_count_non_negative CHECK (error_count >= 0),
    ADD CONSTRAINT IF NOT EXISTS chk_api_keys_rate_limit_tier CHECK (rate_limit_tier IN ('free', 'standard', 'premium', 'enterprise', 'unlimited'));

-- ============================================================================
-- API Key Usage Logs Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS api_key_usage_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    api_key_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    request_id VARCHAR(256),
    endpoint VARCHAR(512) NOT NULL,
    method VARCHAR(10) NOT NULL,
    status_code INTEGER,
    input_tokens INTEGER DEFAULT 0,
    output_tokens INTEGER DEFAULT 0,
    total_tokens INTEGER DEFAULT 0,
    cost DECIMAL(18,8) DEFAULT 0.0,
    latency_ms INTEGER,
    client_ip VARCHAR(45),
    user_agent TEXT,
    origin VARCHAR(512),
    model VARCHAR(256),
    provider VARCHAR(256),
    was_cached BOOLEAN DEFAULT FALSE,
    cache_hit BOOLEAN DEFAULT FALSE,
    error_code VARCHAR(100),
    error_message TEXT,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- API Key Usage Logs indexes
CREATE INDEX IF NOT EXISTS idx_api_key_usage_api_key_id ON api_key_usage_logs(api_key_id);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_org_id ON api_key_usage_logs(organization_id);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_request_id ON api_key_usage_logs(request_id);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_endpoint ON api_key_usage_logs(endpoint);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_status ON api_key_usage_logs(status_code);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_created_at ON api_key_usage_logs(created_at);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_model ON api_key_usage_logs(model);

-- Partition by date for large tables
-- Note: Partitioning requires more complex setup, using regular table for now
-- Consider partitioning by created_at for high-volume scenarios

-- API Key Usage Logs constraints
ALTER TABLE api_key_usage_logs
    ADD CONSTRAINT IF NOT EXISTS chk_api_key_usage_tokens_non_negative 
        CHECK (input_tokens >= 0 AND output_tokens >= 0 AND total_tokens >= 0),
    ADD CONSTRAINT IF NOT EXISTS chk_api_key_usage_cost_non_negative CHECK (cost >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_api_key_usage_method CHECK (method IN ('GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS')),
    ADD CONSTRAINT IF NOT EXISTS fk_api_key_usage_api_key
        FOREIGN KEY (api_key_id) REFERENCES api_keys(id) ON DELETE CASCADE;

-- ============================================================================
-- Rate Limit Configuration Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS rate_limit_configs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(256) NOT NULL UNIQUE,
    description TEXT,
    tier VARCHAR(50) NOT NULL DEFAULT 'standard',
    scope VARCHAR(50) NOT NULL DEFAULT 'organization', -- organization, team, user, api_key
    
    -- Request-based limits
    requests_per_second INTEGER DEFAULT 10,
    requests_per_minute INTEGER DEFAULT 100,
    requests_per_hour INTEGER DEFAULT 1000,
    requests_per_day INTEGER DEFAULT 10000,
    
    -- Token-based limits
    tokens_per_minute INTEGER DEFAULT 100000,
    tokens_per_hour INTEGER DEFAULT 1000000,
    tokens_per_day INTEGER DEFAULT 10000000,
    
    -- Cost-based limits
    cost_per_hour DECIMAL(18,8) DEFAULT 100.0,
    cost_per_day DECIMAL(18,8) DEFAULT 1000.0,
    cost_per_month DECIMAL(18,8) DEFAULT 10000.0,
    
    -- Concurrency limits
    max_concurrent_requests INTEGER DEFAULT 10,
    max_queue_wait_ms INTEGER DEFAULT 30000,
    
    -- Window configuration
    window_type VARCHAR(50) DEFAULT 'sliding', -- fixed, sliding, token_bucket
    burst_capacity INTEGER DEFAULT 20,
    
    -- Behavior
    action_on_limit VARCHAR(50) DEFAULT 'reject', -- reject, queue, throttle
    queue_max_size INTEGER DEFAULT 100,
    throttle_rate_percent INTEGER DEFAULT 50,
    
    -- Priority handling
    priority_enabled BOOLEAN DEFAULT FALSE,
    priority_levels INTEGER DEFAULT 3,
    
    is_active BOOLEAN DEFAULT TRUE,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_by UUID,
    effective_from TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    effective_until TIMESTAMP WITH TIME ZONE
);

-- Rate Limit Config indexes
CREATE INDEX IF NOT EXISTS idx_rate_limit_configs_tier ON rate_limit_configs(tier);
CREATE INDEX IF NOT EXISTS idx_rate_limit_configs_scope ON rate_limit_configs(scope);
CREATE INDEX IF NOT EXISTS idx_rate_limit_configs_active ON rate_limit_configs(is_active);
CREATE INDEX IF NOT EXISTS idx_rate_limit_configs_effective ON rate_limit_configs(effective_from, effective_until);

-- Rate Limit Config constraints
ALTER TABLE rate_limit_configs
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_tier CHECK (tier IN ('free', 'standard', 'premium', 'enterprise', 'unlimited', 'custom')),
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_scope CHECK (scope IN ('global', 'organization', 'team', 'user', 'api_key')),
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_window_type CHECK (window_type IN ('fixed', 'sliding', 'token_bucket')),
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_action CHECK (action_on_limit IN ('reject', 'queue', 'throttle', 'allow')),
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_requests_per_second_positive CHECK (requests_per_second > 0),
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_requests_per_minute_positive CHECK (requests_per_minute > 0),
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_tokens_per_minute_positive CHECK (tokens_per_minute > 0),
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_cost_per_hour_non_negative CHECK (cost_per_hour >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_max_concurrent_positive CHECK (max_concurrent_requests > 0),
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_burst_capacity_positive CHECK (burst_capacity > 0);

-- ============================================================================
-- Rate Limit Assignments Table (linking configs to entities)
-- ============================================================================
CREATE TABLE IF NOT EXISTS rate_limit_assignments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    config_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    team_id UUID,
    user_id UUID,
    api_key_id UUID,
    priority INTEGER DEFAULT 1,
    is_active BOOLEAN DEFAULT TRUE,
    effective_from TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    effective_until TIMESTAMP WITH TIME ZONE,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_by UUID
);

-- Rate Limit Assignments indexes
CREATE INDEX IF NOT EXISTS idx_rate_limit_assign_config ON rate_limit_assignments(config_id);
CREATE INDEX IF NOT EXISTS idx_rate_limit_assign_org ON rate_limit_assignments(organization_id);
CREATE INDEX IF NOT EXISTS idx_rate_limit_assign_team ON rate_limit_assignments(team_id);
CREATE INDEX IF NOT EXISTS idx_rate_limit_assign_user ON rate_limit_assignments(user_id);
CREATE INDEX IF NOT EXISTS idx_rate_limit_assign_api_key ON rate_limit_assignments(api_key_id);
CREATE INDEX IF NOT EXISTS idx_rate_limit_assign_active ON rate_limit_assignments(is_active);
CREATE INDEX IF NOT EXISTS idx_rate_limit_assign_effective ON rate_limit_assignments(effective_from, effective_until);

-- Rate Limit Assignments constraints
ALTER TABLE rate_limit_assignments
    ADD CONSTRAINT IF NOT EXISTS fk_rate_limit_assign_config
        FOREIGN KEY (config_id) REFERENCES rate_limit_configs(id) ON DELETE CASCADE,
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_assign_priority_positive CHECK (priority > 0);

-- ============================================================================
-- Rate Limit Tracking Table (for current window tracking)
-- ============================================================================
CREATE TABLE IF NOT EXISTS rate_limit_tracking (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    entity_type VARCHAR(50) NOT NULL, -- organization, team, user, api_key
    entity_id UUID NOT NULL,
    window_start TIMESTAMP WITH TIME ZONE NOT NULL,
    window_end TIMESTAMP WITH TIME ZONE NOT NULL,
    window_type VARCHAR(50) NOT NULL,
    
    -- Current counts
    request_count INTEGER DEFAULT 0,
    token_count BIGINT DEFAULT 0,
    cost_amount DECIMAL(18,8) DEFAULT 0.0,
    
    -- Burst tracking for token bucket
    tokens_available DECIMAL(18,4) DEFAULT 0.0,
    last_refill_at TIMESTAMP WITH TIME ZONE,
    
    -- Concurrency tracking
    concurrent_requests INTEGER DEFAULT 0,
    queued_requests INTEGER DEFAULT 0,
    
    -- Metadata
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Rate Limit Tracking indexes
CREATE INDEX IF NOT EXISTS idx_rate_limit_track_entity ON rate_limit_tracking(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS idx_rate_limit_track_window ON rate_limit_tracking(window_start, window_end);
CREATE INDEX IF NOT EXISTS idx_rate_limit_track_active ON rate_limit_tracking(window_end) WHERE window_end > NOW();

-- Rate Limit Tracking constraints
ALTER TABLE rate_limit_tracking
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_track_entity_type CHECK (entity_type IN ('organization', 'team', 'user', 'api_key')),
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_track_request_count_non_negative CHECK (request_count >= 0),
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_track_token_count_non_negative CHECK (token_count >= 0),
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_track_cost_non_negative CHECK (cost_amount >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_track_concurrent_non_negative CHECK (concurrent_requests >= 0),
    ADD CONSTRAINT IF NOT EXISTS chk_rate_limit_track_queued_non_negative CHECK (queued_requests >= 0);

-- ============================================================================
-- API Endpoints Registry Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS api_endpoints (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    path VARCHAR(512) NOT NULL,
    method VARCHAR(10) NOT NULL,
    name VARCHAR(256) NOT NULL,
    description TEXT,
    category VARCHAR(100),
    version VARCHAR(20) DEFAULT 'v1',
    is_deprecated BOOLEAN DEFAULT FALSE,
    deprecation_date TIMESTAMP WITH TIME ZONE,
    requires_auth BOOLEAN DEFAULT TRUE,
    required_permissions TEXT[] DEFAULT '{}',
    rate_limit_override UUID, -- References rate_limit_configs
    request_schema JSONB,
    response_schema JSONB,
    example_request JSONB,
    example_response JSONB,
    documentation_url VARCHAR(512),
    tags TEXT[] DEFAULT '{}',
    is_active BOOLEAN DEFAULT TRUE,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- API Endpoints indexes
CREATE UNIQUE INDEX IF NOT EXISTS idx_api_endpoints_unique ON api_endpoints(path, method, version);
CREATE INDEX IF NOT EXISTS idx_api_endpoints_category ON api_endpoints(category);
CREATE INDEX IF NOT EXISTS idx_api_endpoints_active ON api_endpoints(is_active);
CREATE INDEX IF NOT EXISTS idx_api_endpoints_deprecated ON api_endpoints(is_deprecated) WHERE is_deprecated = TRUE;

-- API Endpoints constraints
ALTER TABLE api_endpoints
    ADD CONSTRAINT IF NOT EXISTS chk_api_endpoints_method CHECK (method IN ('GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS', 'TRACE', 'CONNECT')),
    ADD CONSTRAINT IF NOT EXISTS fk_api_endpoints_rate_limit
        FOREIGN KEY (rate_limit_override) REFERENCES rate_limit_configs(id) ON DELETE SET NULL;

-- ============================================================================
-- Triggers
-- ============================================================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_api_keys_updated_at') THEN
        CREATE TRIGGER trg_api_keys_updated_at
        BEFORE UPDATE ON api_keys
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_rate_limit_configs_updated_at') THEN
        CREATE TRIGGER trg_rate_limit_configs_updated_at
        BEFORE UPDATE ON rate_limit_configs
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_rate_limit_assignments_updated_at') THEN
        CREATE TRIGGER trg_rate_limit_assignments_updated_at
        BEFORE UPDATE ON rate_limit_assignments
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_rate_limit_tracking_updated_at') THEN
        CREATE TRIGGER trg_rate_limit_tracking_updated_at
        BEFORE UPDATE ON rate_limit_tracking
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_api_endpoints_updated_at') THEN
        CREATE TRIGGER trg_api_endpoints_updated_at
        BEFORE UPDATE ON api_endpoints
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;
END $$;

-- ============================================================================
-- Migration Completion Log
-- ============================================================================
INSERT INTO schema_migrations (version, applied_at, description)
VALUES ('003', NOW(), 'ApiManagementTables: api_keys, api_key_usage_logs, rate_limit_configs, rate_limit_assignments, rate_limit_tracking, api_endpoints')
ON CONFLICT (version) DO UPDATE SET
    applied_at = EXCLUDED.applied_at,
    description = EXCLUDED.description;
