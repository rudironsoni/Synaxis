-- Migration: 001_InitialInferenceSchema
-- Description: Initial schema for Inference domain - ChatTemplate, ModelConfig, UserChatPreferences
-- Idempotent: Yes
-- Created: 2026-03-04

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================================
-- ChatTemplate Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS chat_templates (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL,
    name VARCHAR(256) NOT NULL,
    description TEXT,
    system_prompt TEXT NOT NULL,
    user_prompt_template TEXT NOT NULL,
    variables JSONB DEFAULT '{}',
    is_active BOOLEAN DEFAULT TRUE,
    is_system BOOLEAN DEFAULT FALSE,
    created_by UUID,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    deleted_at TIMESTAMP WITH TIME ZONE
);

-- ChatTemplate indexes
CREATE INDEX IF NOT EXISTS idx_chat_templates_org_id ON chat_templates(organization_id);
CREATE INDEX IF NOT EXISTS idx_chat_templates_active ON chat_templates(is_active);
CREATE INDEX IF NOT EXISTS idx_chat_templates_system ON chat_templates(is_system);
CREATE INDEX IF NOT EXISTS idx_chat_templates_created_at ON chat_templates(created_at);

-- ChatTemplate constraints
ALTER TABLE chat_templates
    ADD CONSTRAINT IF NOT EXISTS chk_chat_template_name_not_empty CHECK (LENGTH(TRIM(name)) > 0),
    ADD CONSTRAINT IF NOT EXISTS chk_chat_template_system_prompt_not_empty CHECK (LENGTH(TRIM(system_prompt)) > 0);

-- ============================================================================
-- ModelConfig Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS model_configs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID,
    model_id VARCHAR(256) NOT NULL,
    provider_id VARCHAR(256) NOT NULL,
    display_name VARCHAR(256) NOT NULL,
    description TEXT,
    max_tokens INTEGER DEFAULT 4096,
    temperature DECIMAL(4,3) DEFAULT 0.7,
    top_p DECIMAL(4,3) DEFAULT 1.0,
    top_k INTEGER DEFAULT NULL,
    presence_penalty DECIMAL(4,3) DEFAULT 0.0,
    frequency_penalty DECIMAL(4,3) DEFAULT 0.0,
    input_price_per_1k DECIMAL(18,8) DEFAULT 0.0,
    output_price_per_1k DECIMAL(18,8) DEFAULT 0.0,
    is_active BOOLEAN DEFAULT TRUE,
    is_default BOOLEAN DEFAULT FALSE,
    capabilities JSONB DEFAULT '[]',
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    deleted_at TIMESTAMP WITH TIME ZONE
);

-- ModelConfig indexes
CREATE INDEX IF NOT EXISTS idx_model_configs_org_id ON model_configs(organization_id);
CREATE INDEX IF NOT EXISTS idx_model_configs_model_id ON model_configs(model_id);
CREATE INDEX IF NOT EXISTS idx_model_configs_provider_id ON model_configs(provider_id);
CREATE INDEX IF NOT EXISTS idx_model_configs_active ON model_configs(is_active);
CREATE INDEX IF NOT EXISTS idx_model_configs_default ON model_configs(is_default) WHERE is_default = TRUE;

-- ModelConfig constraints
ALTER TABLE model_configs
    ADD CONSTRAINT IF NOT EXISTS chk_model_config_max_tokens_positive CHECK (max_tokens > 0),
    ADD CONSTRAINT IF NOT EXISTS chk_model_config_temperature_range CHECK (temperature >= 0.0 AND temperature <= 2.0),
    ADD CONSTRAINT IF NOT EXISTS chk_model_config_top_p_range CHECK (top_p >= 0.0 AND top_p <= 1.0),
    ADD CONSTRAINT IF NOT EXISTS chk_model_config_presence_penalty_range CHECK (presence_penalty >= -2.0 AND presence_penalty <= 2.0),
    ADD CONSTRAINT IF NOT EXISTS chk_model_config_frequency_penalty_range CHECK (frequency_penalty >= -2.0 AND frequency_penalty <= 2.0),
    ADD CONSTRAINT IF NOT EXISTS chk_model_config_input_price_non_negative CHECK (input_price_per_1k >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_model_config_output_price_non_negative CHECK (output_price_per_1k >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS uq_model_config_org_model_provider UNIQUE NULLS NOT DISTINCT (organization_id, model_id, provider_id);

-- ============================================================================
-- UserChatPreferences Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_chat_preferences (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    default_model_config_id UUID,
    default_chat_template_id UUID,
    preferred_temperature DECIMAL(4,3) DEFAULT 0.7,
    preferred_max_tokens INTEGER DEFAULT 4096,
    auto_save_chats BOOLEAN DEFAULT TRUE,
    enable_streaming BOOLEAN DEFAULT TRUE,
    theme_preference VARCHAR(50) DEFAULT 'system',
    language_preference VARCHAR(10) DEFAULT 'en',
    timezone VARCHAR(100) DEFAULT 'UTC',
    notification_settings JSONB DEFAULT '{}',
    privacy_settings JSONB DEFAULT '{}',
    custom_settings JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- UserChatPreferences indexes
CREATE INDEX IF NOT EXISTS idx_user_chat_prefs_user_id ON user_chat_preferences(user_id);
CREATE INDEX IF NOT EXISTS idx_user_chat_prefs_org_id ON user_chat_preferences(organization_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_user_chat_prefs_user_org_unique ON user_chat_preferences(user_id, organization_id);

-- UserChatPreferences constraints
ALTER TABLE user_chat_preferences
    ADD CONSTRAINT IF NOT EXISTS chk_user_chat_prefs_temperature_range CHECK (preferred_temperature >= 0.0 AND preferred_temperature <= 2.0),
    ADD CONSTRAINT IF NOT EXISTS chk_user_chat_prefs_max_tokens_positive CHECK (preferred_max_tokens > 0),
    ADD CONSTRAINT IF NOT EXISTS chk_user_chat_prefs_theme CHECK (theme_preference IN ('light', 'dark', 'system')),
    ADD CONSTRAINT IF NOT EXISTS fk_user_chat_prefs_model_config
        FOREIGN KEY (default_model_config_id) REFERENCES model_configs(id) ON DELETE SET NULL,
    ADD CONSTRAINT IF NOT EXISTS fk_user_chat_prefs_chat_template
        FOREIGN KEY (default_chat_template_id) REFERENCES chat_templates(id) ON DELETE SET NULL;

-- ============================================================================
-- InferenceRequest Table (for inference history)
-- ============================================================================
CREATE TABLE IF NOT EXISTS inference_requests (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    request_id VARCHAR(256) NOT NULL UNIQUE,
    organization_id UUID NOT NULL,
    user_id UUID,
    team_id UUID,
    virtual_key_id UUID,
    model_config_id UUID,
    chat_template_id UUID,
    model VARCHAR(256) NOT NULL,
    provider VARCHAR(256) NOT NULL,
    request_content TEXT NOT NULL,
    response_content TEXT,
    input_tokens INTEGER DEFAULT 0,
    output_tokens INTEGER DEFAULT 0,
    total_tokens INTEGER DEFAULT 0,
    cost DECIMAL(18,8) DEFAULT 0.0,
    duration_ms INTEGER,
    queue_time_ms INTEGER DEFAULT 0,
    status VARCHAR(50) DEFAULT 'pending',
    error_message TEXT,
    error_code VARCHAR(100),
    request_headers JSONB DEFAULT '{}',
    response_headers JSONB DEFAULT '{}',
    metadata JSONB DEFAULT '{}',
    user_region VARCHAR(50),
    processed_region VARCHAR(50),
    cross_border_transfer BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    completed_at TIMESTAMP WITH TIME ZONE,
    deleted_at TIMESTAMP WITH TIME ZONE
);

-- InferenceRequest indexes
CREATE INDEX IF NOT EXISTS idx_inference_requests_org_id ON inference_requests(organization_id);
CREATE INDEX IF NOT EXISTS idx_inference_requests_user_id ON inference_requests(user_id);
CREATE INDEX IF NOT EXISTS idx_inference_requests_team_id ON inference_requests(team_id);
CREATE INDEX IF NOT EXISTS idx_inference_requests_model ON inference_requests(model);
CREATE INDEX IF NOT EXISTS idx_inference_requests_status ON inference_requests(status);
CREATE INDEX IF NOT EXISTS idx_inference_requests_created_at ON inference_requests(created_at);
CREATE INDEX IF NOT EXISTS idx_inference_requests_request_id ON inference_requests(request_id);

-- InferenceRequest constraints
ALTER TABLE inference_requests
    ADD CONSTRAINT IF NOT EXISTS chk_inference_status CHECK (status IN ('pending', 'processing', 'completed', 'failed', 'cancelled')),
    ADD CONSTRAINT IF NOT EXISTS chk_inference_input_tokens_non_negative CHECK (input_tokens >= 0),
    ADD CONSTRAINT IF NOT EXISTS chk_inference_output_tokens_non_negative CHECK (output_tokens >= 0),
    ADD CONSTRAINT IF NOT EXISTS chk_inference_cost_non_negative CHECK (cost >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS fk_inference_model_config
        FOREIGN KEY (model_config_id) REFERENCES model_configs(id) ON DELETE SET NULL,
    ADD CONSTRAINT IF NOT EXISTS fk_inference_chat_template
        FOREIGN KEY (chat_template_id) REFERENCES chat_templates(id) ON DELETE SET NULL;

-- ============================================================================
-- Update triggers for updated_at
-- ============================================================================
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Attach triggers
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_chat_templates_updated_at') THEN
        CREATE TRIGGER trg_chat_templates_updated_at
        BEFORE UPDATE ON chat_templates
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_model_configs_updated_at') THEN
        CREATE TRIGGER trg_model_configs_updated_at
        BEFORE UPDATE ON model_configs
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_user_chat_preferences_updated_at') THEN
        CREATE TRIGGER trg_user_chat_preferences_updated_at
        BEFORE UPDATE ON user_chat_preferences
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;
END $$;

-- ============================================================================
-- Migration Completion Log
-- ============================================================================
INSERT INTO schema_migrations (version, applied_at, description)
VALUES ('001', NOW(), 'InitialInferenceSchema: ChatTemplate, ModelConfig, UserChatPreferences, InferenceRequest')
ON CONFLICT (version) DO UPDATE SET
    applied_at = EXCLUDED.applied_at,
    description = EXCLUDED.description;
