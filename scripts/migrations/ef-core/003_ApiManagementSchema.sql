-- =============================================================================
-- Migration: 003_ApiManagementSchema
-- Description: API Management tables for rate limiting, quotas, and gateway
-- Created: 2026-03-04
-- Idempotent: Yes
-- Dependencies: 001_InitialSchema
-- =============================================================================

-- =============================================================================
-- API GATEWAY TABLES
-- =============================================================================

-- API Endpoints table
CREATE TABLE IF NOT EXISTS api_endpoints (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    path VARCHAR(500) NOT NULL,
    method VARCHAR(10) NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    requires_authentication BOOLEAN NOT NULL DEFAULT true,
    allowed_roles TEXT[] NOT NULL DEFAULT '{}',
    rate_limit_per_minute INTEGER,
    rate_limit_per_hour INTEGER,
    rate_limit_per_day INTEGER,
    metadata JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_api_endpoints_org_path_method UNIQUE (organization_id, path, method)
);

CREATE INDEX IF NOT EXISTS idx_api_endpoints_org ON api_endpoints(organization_id);
CREATE INDEX IF NOT EXISTS idx_api_endpoints_active ON api_endpoints(is_active);

-- API Rate Limits table
CREATE TABLE IF NOT EXISTS api_rate_limits (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    scope_type VARCHAR(50) NOT NULL, -- 'organization', 'team', 'user', 'key'
    scope_id UUID NOT NULL,
    endpoint_id UUID REFERENCES api_endpoints(id) ON DELETE SET NULL,
    requests_per_minute INTEGER,
    requests_per_hour INTEGER,
    requests_per_day INTEGER,
    burst_limit INTEGER,
    is_active BOOLEAN NOT NULL DEFAULT true,
    effective_from TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    effective_until TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_api_rate_limits_org ON api_rate_limits(organization_id);
CREATE INDEX IF NOT EXISTS idx_api_rate_limits_scope ON api_rate_limits(scope_type, scope_id);
CREATE INDEX IF NOT EXISTS idx_api_rate_limits_endpoint ON api_rate_limits(endpoint_id);
CREATE INDEX IF NOT EXISTS idx_api_rate_limits_active ON api_rate_limits(is_active, effective_from, effective_until);

-- API Quotas table
CREATE TABLE IF NOT EXISTS api_quotas (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    scope_type VARCHAR(50) NOT NULL, -- 'organization', 'team', 'user', 'key'
    scope_id UUID NOT NULL,
    quota_type VARCHAR(50) NOT NULL, -- 'requests', 'tokens', 'cost'
    limit_value DECIMAL(18, 8) NOT NULL,
    period VARCHAR(20) NOT NULL, -- 'minute', 'hour', 'day', 'week', 'month'
    current_usage DECIMAL(18, 8) NOT NULL DEFAULT 0,
    period_start TIMESTAMPTZ NOT NULL,
    period_end TIMESTAMPTZ NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_api_quotas_org ON api_quotas(organization_id);
CREATE INDEX IF NOT EXISTS idx_api_quotas_scope ON api_quotas(scope_type, scope_id);
CREATE INDEX IF NOT EXISTS idx_api_quotas_period ON api_quotas(period_start, period_end);
CREATE INDEX IF NOT EXISTS idx_api_quotas_active ON api_quotas(is_active);

-- API Usage Records table
CREATE TABLE IF NOT EXISTS api_usage_records (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    scope_type VARCHAR(50) NOT NULL,
    scope_id UUID NOT NULL,
    endpoint_id UUID REFERENCES api_endpoints(id) ON DELETE SET NULL,
    request_count BIGINT NOT NULL DEFAULT 0,
    token_count BIGINT NOT NULL DEFAULT 0,
    cost_usd DECIMAL(18, 8) NOT NULL DEFAULT 0,
    period_start TIMESTAMPTZ NOT NULL,
    period_end TIMESTAMPTZ NOT NULL,
    granularity VARCHAR(20) NOT NULL, -- 'minute', 'hour', 'day'
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_api_usage_org ON api_usage_records(organization_id);
CREATE INDEX IF NOT EXISTS idx_api_usage_scope_period ON api_usage_records(scope_type, scope_id, period_start, period_end);
CREATE INDEX IF NOT EXISTS idx_api_usage_endpoint ON api_usage_records(endpoint_id);
CREATE INDEX IF NOT EXISTS idx_api_usage_granularity ON api_usage_records(granularity);

-- =============================================================================
-- API KEY MANAGEMENT TABLES
-- =============================================================================

-- API Key Scopes table (for granular permissions)
CREATE TABLE IF NOT EXISTS api_key_scopes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key_id UUID NOT NULL REFERENCES virtual_keys(id) ON DELETE CASCADE,
    scope VARCHAR(255) NOT NULL,
    resource_type VARCHAR(100),
    resource_id UUID,
    permissions TEXT[] NOT NULL DEFAULT '{"read"}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_api_key_scopes_key_scope UNIQUE (key_id, scope, resource_type, resource_id)
);

CREATE INDEX IF NOT EXISTS idx_api_key_scopes_key ON api_key_scopes(key_id);
CREATE INDEX IF NOT EXISTS idx_api_key_scopes_scope ON api_key_scopes(scope);

-- API Key Rate Limit Windows table (sliding window tracking)
CREATE TABLE IF NOT EXISTS api_key_rate_windows (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key_id UUID NOT NULL REFERENCES virtual_keys(id) ON DELETE CASCADE,
    window_type VARCHAR(20) NOT NULL, -- 'minute', 'hour', 'day'
    window_start TIMESTAMPTZ NOT NULL,
    request_count INTEGER NOT NULL DEFAULT 0,
    token_count BIGINT NOT NULL DEFAULT 0,
    cost_usd DECIMAL(18, 8) NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_api_key_rate_windows_key_type_start UNIQUE (key_id, window_type, window_start)
);

CREATE INDEX IF NOT EXISTS idx_api_key_rate_windows_key ON api_key_rate_windows(key_id);
CREATE INDEX IF NOT EXISTS idx_api_key_rate_windows_type_start ON api_key_rate_windows(window_type, window_start);
CREATE INDEX IF NOT EXISTS idx_api_key_rate_windows_cleanup ON api_key_rate_windows(window_start);

-- =============================================================================
-- GATEWAY ROUTING TABLES
-- =============================================================================

-- Gateway Routes table
CREATE TABLE IF NOT EXISTS gateway_routes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    source_path VARCHAR(500) NOT NULL,
    target_url TEXT NOT NULL,
    method VARCHAR(10) NOT NULL DEFAULT 'ANY',
    priority INTEGER NOT NULL DEFAULT 100,
    is_active BOOLEAN NOT NULL DEFAULT true,
    transform_request JSONB,
    transform_response JSONB,
    retry_policy JSONB,
    timeout_seconds INTEGER DEFAULT 30,
    circuit_breaker JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_gateway_routes_org ON gateway_routes(organization_id);
CREATE INDEX IF NOT EXISTS idx_gateway_routes_priority ON gateway_routes(priority DESC);
CREATE INDEX IF NOT EXISTS idx_gateway_routes_active ON gateway_routes(is_active);

-- Gateway Upstreams table
CREATE TABLE IF NOT EXISTS gateway_upstreams (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    target_host TEXT NOT NULL,
    target_port INTEGER NOT NULL DEFAULT 443,
    protocol VARCHAR(10) NOT NULL DEFAULT 'https',
    health_check_path VARCHAR(255) DEFAULT '/health',
    health_check_interval_seconds INTEGER DEFAULT 30,
    is_healthy BOOLEAN NOT NULL DEFAULT true,
    last_health_check TIMESTAMPTZ,
    weight INTEGER NOT NULL DEFAULT 100,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_gateway_upstreams_org ON gateway_upstreams(organization_id);
CREATE INDEX IF NOT EXISTS idx_gateway_upstreams_healthy ON gateway_upstreams(is_healthy, is_active);

-- =============================================================================
-- API METRICS & ANALYTICS TABLES
-- =============================================================================

-- API Latency Metrics table
CREATE TABLE IF NOT EXISTS api_latency_metrics (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    endpoint_id UUID REFERENCES api_endpoints(id) ON DELETE SET NULL,
    granularity VARCHAR(20) NOT NULL, -- 'minute', 'hour', 'day'
    period_start TIMESTAMPTZ NOT NULL,
    p50_ms INTEGER NOT NULL DEFAULT 0,
    p95_ms INTEGER NOT NULL DEFAULT 0,
    p99_ms INTEGER NOT NULL DEFAULT 0,
    avg_ms INTEGER NOT NULL DEFAULT 0,
    max_ms INTEGER NOT NULL DEFAULT 0,
    min_ms INTEGER NOT NULL DEFAULT 0,
    sample_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_api_latency_org ON api_latency_metrics(organization_id);
CREATE INDEX IF NOT EXISTS idx_api_latency_endpoint ON api_latency_metrics(endpoint_id);
CREATE INDEX IF NOT EXISTS idx_api_latency_period ON api_latency_metrics(period_start, granularity);

-- API Error Rates table
CREATE TABLE IF NOT EXISTS api_error_rates (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    endpoint_id UUID REFERENCES api_endpoints(id) ON DELETE SET NULL,
    granularity VARCHAR(20) NOT NULL,
    period_start TIMESTAMPTZ NOT NULL,
    status_code INTEGER,
    error_count INTEGER NOT NULL DEFAULT 0,
    total_count INTEGER NOT NULL DEFAULT 0,
    error_rate DECIMAL(5, 4) NOT NULL DEFAULT 0, -- percentage 0.0000 to 1.0000
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_api_error_rates_org ON api_error_rates(organization_id);
CREATE INDEX IF NOT EXISTS idx_api_error_rates_endpoint ON api_error_rates(endpoint_id);
CREATE INDEX IF NOT EXISTS idx_api_error_rates_period ON api_error_rates(period_start, granularity);

-- =============================================================================
-- API MANAGEMENT FUNCTIONS
-- =============================================================================

-- Function to check rate limit
CREATE OR REPLACE FUNCTION check_rate_limit(
    p_scope_type VARCHAR(50),
    p_scope_id UUID,
    p_endpoint_id UUID DEFAULT NULL
)
RETURNS TABLE (
    allowed BOOLEAN,
    remaining INTEGER,
    reset_at TIMESTAMPTZ,
    limit_value INTEGER
) AS $$
DECLARE
    v_limit INTEGER;
    v_used INTEGER;
    v_window_start TIMESTAMPTZ;
    v_window_end TIMESTAMPTZ;
BEGIN
    -- Get applicable rate limit
    SELECT COALESCE(
        (SELECT requests_per_minute
         FROM api_rate_limits
         WHERE scope_type = p_scope_type
           AND scope_id = p_scope_id
           AND (endpoint_id = p_endpoint_id OR endpoint_id IS NULL)
           AND is_active = true
           AND effective_from <= NOW()
           AND (effective_until IS NULL OR effective_until > NOW())
         ORDER BY endpoint_id NULLS LAST
         LIMIT 1),
        1000 -- Default limit
    ) INTO v_limit;

    -- Calculate window boundaries (per minute)
    v_window_start := DATE_TRUNC('minute', NOW());
    v_window_end := v_window_start + INTERVAL '1 minute';

    -- Get current usage in window
    SELECT COALESCE(SUM(request_count), 0)
    INTO v_used
    FROM api_key_rate_windows
    WHERE key_id = p_scope_id
      AND window_type = 'minute'
      AND window_start = v_window_start;

    RETURN QUERY SELECT
        v_used < v_limit,
        GREATEST(0, v_limit - v_used),
        v_window_end,
        v_limit;
END;
$$ LANGUAGE plpgsql STABLE;

-- Function to record API usage
CREATE OR REPLACE FUNCTION record_api_usage(
    p_organization_id UUID,
    p_scope_type VARCHAR(50),
    p_scope_id UUID,
    p_endpoint_id UUID,
    p_token_count BIGINT DEFAULT 0,
    p_cost_usd DECIMAL(18, 8) DEFAULT 0
)
RETURNS VOID AS $$
DECLARE
    v_window_start TIMESTAMPTZ;
BEGIN
    -- Calculate window start
    v_window_start := DATE_TRUNC('minute', NOW());

    -- Update or insert rate window
    INSERT INTO api_key_rate_windows (
        key_id, window_type, window_start, request_count, token_count, cost_usd
    ) VALUES (
        p_scope_id, 'minute', v_window_start, 1, p_token_count, p_cost_usd
    )
    ON CONFLICT (key_id, window_type, window_start) DO UPDATE SET
        request_count = api_key_rate_windows.request_count + 1,
        token_count = api_key_rate_windows.token_count + p_token_count,
        cost_usd = api_key_rate_windows.cost_usd + p_cost_usd,
        updated_at = NOW();
END;
$$ LANGUAGE plpgsql;

-- Function to cleanup old rate windows
CREATE OR REPLACE FUNCTION cleanup_rate_windows(
    p_retention_hours INTEGER DEFAULT 24
)
RETURNS INTEGER AS $$
DECLARE
    v_count INTEGER;
BEGIN
    DELETE FROM api_key_rate_windows
    WHERE window_start < NOW() - (p_retention_hours || ' hours')::INTERVAL;

    GET DIAGNOSTICS v_count = ROW_COUNT;
    RETURN v_count;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- MIGRATION METADATA
-- =============================================================================

-- Record this migration
INSERT INTO __migrations_history (migration_id, migration_name, checksum, is_idempotent)
VALUES ('003', 'ApiManagementSchema', MD5(pg_read_file('/scripts/migrations/ef-core/003_ApiManagementSchema.sql')::text), true)
ON CONFLICT (migration_id) DO NOTHING;

-- =============================================================================
-- COMMENTS
-- =============================================================================

COMMENT ON TABLE api_endpoints IS 'API endpoint definitions for gateway routing';
COMMENT ON TABLE api_rate_limits IS 'Rate limit configurations per scope';
COMMENT ON TABLE api_quotas IS 'Usage quotas per scope and period';
COMMENT ON TABLE api_usage_records IS 'Aggregated API usage statistics';
COMMENT ON TABLE api_key_scopes IS 'Granular API key permissions';
COMMENT ON TABLE gateway_routes IS 'Gateway routing rules';
COMMENT ON TABLE gateway_upstreams IS 'Upstream service definitions';
COMMENT ON FUNCTION check_rate_limit IS 'Checks if request is within rate limits';
COMMENT ON FUNCTION record_api_usage IS 'Records API usage for rate limiting and billing';
