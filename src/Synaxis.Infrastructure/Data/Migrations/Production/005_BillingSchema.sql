-- Migration: 005_BillingSchema
-- Description: Subscription, Invoice, Payment tables for billing domain
-- Idempotent: Yes
-- Created: 2026-03-04

-- ============================================================================
-- Plans Table (Subscription Plans)
-- ============================================================================
CREATE TABLE IF NOT EXISTS billing_plans (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    slug VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(256) NOT NULL,
    description TEXT,
    display_order INTEGER DEFAULT 0,
    
    -- Billing configuration
    billing_frequency VARCHAR(50) NOT NULL DEFAULT 'monthly', -- monthly, yearly, quarterly
    base_price_monthly DECIMAL(18,8) NOT NULL,
    base_price_yearly DECIMAL(18,8),
    setup_fee DECIMAL(18,8) DEFAULT 0.0,
    currency VARCHAR(3) DEFAULT 'USD',
    
    -- Trial configuration
    trial_days INTEGER DEFAULT 0,
    trial_requires_payment_method BOOLEAN DEFAULT FALSE,
    
    -- Features (JSON for flexibility)
    features JSONB DEFAULT '[]',
    included_quota JSONB DEFAULT '{}',
    overage_rates JSONB DEFAULT '{}',
    
    -- Limits
    max_users INTEGER,
    max_teams INTEGER,
    max_api_keys INTEGER,
    max_requests_per_month BIGINT,
    max_storage_gb INTEGER,
    
    -- Plan type
    is_public BOOLEAN DEFAULT TRUE,
    is_active BOOLEAN DEFAULT TRUE,
    is_custom BOOLEAN DEFAULT FALSE,
    is_enterprise BOOLEAN DEFAULT FALSE,
    
    -- Legacy mapping
    legacy_plan_id VARCHAR(256),
    
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    archived_at TIMESTAMP WITH TIME ZONE,
    created_by UUID
);

-- Billing Plans indexes
CREATE INDEX IF NOT EXISTS idx_billing_plans_slug ON billing_plans(slug);
CREATE INDEX IF NOT EXISTS idx_billing_plans_active ON billing_plans(is_active);
CREATE INDEX IF NOT EXISTS idx_billing_plans_public ON billing_plans(is_public);
CREATE INDEX IF NOT EXISTS idx_billing_plans_display_order ON billing_plans(display_order);

-- Billing Plans constraints
ALTER TABLE billing_plans
    ADD CONSTRAINT IF NOT EXISTS chk_billing_plans_frequency CHECK (billing_frequency IN ('monthly', 'yearly', 'quarterly', 'weekly', 'daily')),
    ADD CONSTRAINT IF NOT EXISTS chk_billing_plans_base_price_monthly_non_negative CHECK (base_price_monthly >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_billing_plans_base_price_yearly_non_negative CHECK (base_price_yearly IS NULL OR base_price_yearly >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_billing_plans_setup_fee_non_negative CHECK (setup_fee >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_billing_plans_trial_days_non_negative CHECK (trial_days >= 0),
    ADD CONSTRAINT IF NOT EXISTS chk_billing_plans_max_users_positive CHECK (max_users IS NULL OR max_users > 0),
    ADD CONSTRAINT IF NOT EXISTS chk_billing_plans_max_teams_positive CHECK (max_teams IS NULL OR max_teams > 0);

-- ============================================================================
-- Subscriptions Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS subscriptions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    subscription_number VARCHAR(100) NOT NULL UNIQUE,
    organization_id UUID NOT NULL,
    plan_id UUID NOT NULL,
    
    -- Subscription status
    status VARCHAR(50) NOT NULL DEFAULT 'incomplete', -- incomplete, active, past_due, canceled, paused, trialing
    previous_status VARCHAR(50),
    
    -- Billing cycle
    current_period_start TIMESTAMP WITH TIME ZONE,
    current_period_end TIMESTAMP WITH TIME ZONE,
    trial_start TIMESTAMP WITH TIME ZONE,
    trial_end TIMESTAMP WITH TIME ZONE,
    canceled_at TIMESTAMP WITH TIME ZONE,
    cancellation_reason TEXT,
    cancel_at_period_end BOOLEAN DEFAULT FALSE,
    ended_at TIMESTAMP WITH TIME ZONE,
    
    -- Payment configuration
    payment_method VARCHAR(50) DEFAULT 'card', -- card, ach, wire, invoice
    billing_email VARCHAR(256),
    billing_name VARCHAR(256),
    billing_address JSONB,
    tax_exempt BOOLEAN DEFAULT FALSE,
    tax_id VARCHAR(100),
    tax_rate DECIMAL(5,4) DEFAULT 0.0,
    
    -- Pricing (may differ from plan if custom)
    unit_price DECIMAL(18,8),
    quantity INTEGER DEFAULT 1,
    discount_amount DECIMAL(18,8) DEFAULT 0.0,
    discount_percent DECIMAL(5,2) DEFAULT 0.0,
    
    -- Usage tracking for current period
    usage_current_period JSONB DEFAULT '{}',
    
    -- Automation
    auto_collection BOOLEAN DEFAULT TRUE,
    days_until_due INTEGER DEFAULT 30,
    
    -- Legacy mapping
    legacy_subscription_id VARCHAR(256),
    
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_by UUID
);

-- Subscriptions indexes
CREATE INDEX IF NOT EXISTS idx_subscriptions_org ON subscriptions(organization_id);
CREATE INDEX IF NOT EXISTS idx_subscriptions_plan ON subscriptions(plan_id);
CREATE INDEX IF NOT EXISTS idx_subscriptions_status ON subscriptions(status);
CREATE INDEX IF NOT EXISTS idx_subscriptions_period_end ON subscriptions(current_period_end);
CREATE INDEX IF NOT EXISTS idx_subscriptions_number ON subscriptions(subscription_number);
CREATE INDEX IF NOT EXISTS idx_subscriptions_active ON subscriptions(organization_id, status) WHERE status IN ('active', 'trialing', 'past_due');

-- Subscriptions constraints
ALTER TABLE subscriptions
    ADD CONSTRAINT IF NOT EXISTS chk_subscriptions_status CHECK (status IN ('incomplete', 'active', 'past_due', 'canceled', 'paused', 'trialing', 'unpaid')),
    ADD CONSTRAINT IF NOT EXISTS chk_subscriptions_quantity_positive CHECK (quantity > 0),
    ADD CONSTRAINT IF NOT EXISTS chk_subscriptions_discount_percent CHECK (discount_percent >= 0.0 AND discount_percent <= 100.0),
    ADD CONSTRAINT IF NOT EXISTS chk_subscriptions_tax_rate CHECK (tax_rate >= 0.0 AND tax_rate <= 1.0),
    ADD CONSTRAINT IF NOT EXISTS fk_subscriptions_plan
        FOREIGN KEY (plan_id) REFERENCES billing_plans(id) ON DELETE RESTRICT;

-- ============================================================================
-- Subscription Items Table (for add-ons and per-seat billing)
-- ============================================================================
CREATE TABLE IF NOT EXISTS subscription_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    subscription_id UUID NOT NULL,
    plan_id UUID, -- NULL if this is an add-on not linked to a plan
    
    item_type VARCHAR(50) NOT NULL DEFAULT 'base', -- base, addon, overage, discount
    description VARCHAR(256) NOT NULL,
    
    -- Pricing
    unit_amount DECIMAL(18,8) NOT NULL,
    quantity INTEGER DEFAULT 1,
    usage_type VARCHAR(50) DEFAULT 'licensed', -- licensed (per-seat) or metered
    
    -- For metered items
    current_usage DECIMAL(18,8) DEFAULT 0.0,
    included_quantity DECIMAL(18,8) DEFAULT 0.0,
    
    is_prorated BOOLEAN DEFAULT FALSE,
    proration_factor DECIMAL(5,4) DEFAULT 1.0,
    
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Subscription Items indexes
CREATE INDEX IF NOT EXISTS idx_subscription_items_subscription ON subscription_items(subscription_id);
CREATE INDEX IF NOT EXISTS idx_subscription_items_type ON subscription_items(item_type);

-- Subscription Items constraints
ALTER TABLE subscription_items
    ADD CONSTRAINT IF NOT EXISTS chk_subscription_items_type CHECK (item_type IN ('base', 'addon', 'overage', 'discount', 'tax', 'proration')),
    ADD CONSTRAINT IF NOT EXISTS chk_subscription_items_usage_type CHECK (usage_type IN ('licensed', 'metered')),
    ADD CONSTRAINT IF NOT EXISTS chk_subscription_items_quantity_positive CHECK (quantity > 0),
    ADD CONSTRAINT IF NOT EXISTS chk_subscription_items_unit_amount_non_negative CHECK (unit_amount >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS fk_subscription_items_subscription
        FOREIGN KEY (subscription_id) REFERENCES subscriptions(id) ON DELETE CASCADE,
    ADD CONSTRAINT IF NOT EXISTS fk_subscription_items_plan
        FOREIGN KEY (plan_id) REFERENCES billing_plans(id) ON DELETE SET NULL;

-- ============================================================================
-- Invoices Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS invoices (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    invoice_number VARCHAR(100) NOT NULL UNIQUE,
    subscription_id UUID,
    organization_id UUID NOT NULL,
    
    -- Invoice status
    status VARCHAR(50) NOT NULL DEFAULT 'draft', -- draft, open, paid, void, uncollectible
    
    -- Billing period
    period_start TIMESTAMP WITH TIME ZONE,
    period_end TIMESTAMP WITH TIME ZONE,
    
    -- Amounts
    subtotal DECIMAL(18,8) NOT NULL DEFAULT 0.0,
    discount_amount DECIMAL(18,8) DEFAULT 0.0,
    tax_amount DECIMAL(18,8) DEFAULT 0.0,
    total DECIMAL(18,8) NOT NULL DEFAULT 0.0,
    amount_paid DECIMAL(18,8) DEFAULT 0.0,
    amount_due DECIMAL(18,8) NOT NULL DEFAULT 0.0,
    amount_remaining DECIMAL(18,8) DEFAULT 0.0,
    
    -- Currency
    currency VARCHAR(3) DEFAULT 'USD',
    exchange_rate DECIMAL(18,8) DEFAULT 1.0,
    
    -- Payment status
    paid BOOLEAN DEFAULT FALSE,
    paid_at TIMESTAMP WITH TIME ZONE,
    due_date TIMESTAMP WITH TIME ZONE,
    
    -- Attempts
    attempted BOOLEAN DEFAULT FALSE,
    attempted_at TIMESTAMP WITH TIME ZONE,
    next_payment_attempt TIMESTAMP WITH TIME ZONE,
    
    -- Collection
    collection_method VARCHAR(50) DEFAULT 'charge_automatically', -- charge_automatically, send_invoice
    billing_reason VARCHAR(50), -- subscription_create, subscription_cycle, subscription_update, manual, etc.
    
    -- Customer info snapshot
    customer_name VARCHAR(256),
    customer_email VARCHAR(256),
    customer_address JSONB,
    
    -- PDF/document
    invoice_pdf_url VARCHAR(512),
    hosted_invoice_url VARCHAR(512),
    
    -- Legacy mapping
    legacy_invoice_id VARCHAR(256),
    
    metadata JSONB DEFAULT '{}',
    notes TEXT,
    footer TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    finalized_at TIMESTAMP WITH TIME ZONE,
    voided_at TIMESTAMP WITH TIME ZONE,
    marked_uncollectible_at TIMESTAMP WITH TIME ZONE
);

-- Invoices indexes
CREATE INDEX IF NOT EXISTS idx_invoices_org ON invoices(organization_id);
CREATE INDEX IF NOT EXISTS idx_invoices_subscription ON invoices(subscription_id);
CREATE INDEX IF NOT EXISTS idx_invoices_status ON invoices(status);
CREATE INDEX IF NOT EXISTS idx_invoices_number ON invoices(invoice_number);
CREATE INDEX IF NOT EXISTS idx_invoices_due_date ON invoices(due_date);
CREATE INDEX IF NOT EXISTS idx_invoices_paid ON invoices(paid);
CREATE INDEX IF NOT EXISTS idx_invoices_period ON invoices(period_start, period_end);
CREATE INDEX IF NOT EXISTS idx_invoices_created ON invoices(created_at);
CREATE INDEX IF NOT EXISTS idx_invoices_overdue ON invoices(due_date, status) WHERE status = 'open' AND due_date < NOW();

-- Invoices constraints
ALTER TABLE invoices
    ADD CONSTRAINT IF NOT EXISTS chk_invoices_status CHECK (status IN ('draft', 'open', 'paid', 'void', 'uncollectible', 'deleted')),
    ADD CONSTRAINT IF NOT EXISTS chk_invoices_collection_method CHECK (collection_method IN ('charge_automatically', 'send_invoice')),
    ADD CONSTRAINT IF NOT EXISTS chk_invoices_subtotal_non_negative CHECK (subtotal >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_invoices_discount_non_negative CHECK (discount_amount >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_invoices_tax_non_negative CHECK (tax_amount >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_invoices_total_non_negative CHECK (total >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_invoices_amount_paid_non_negative CHECK (amount_paid >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS fk_invoices_subscription
        FOREIGN KEY (subscription_id) REFERENCES subscriptions(id) ON DELETE SET NULL;

-- ============================================================================
-- Invoice Line Items Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS invoice_line_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    invoice_id UUID NOT NULL,
    subscription_item_id UUID,
    
    description TEXT NOT NULL,
    
    -- Amounts
    quantity DECIMAL(18,8) DEFAULT 1.0,
    unit_amount DECIMAL(18,8) NOT NULL,
    amount DECIMAL(18,8) NOT NULL,
    
    -- Period this line item covers
    period_start TIMESTAMP WITH TIME ZONE,
    period_end TIMESTAMP WITH TIME ZONE,
    
    -- Proration
    is_prorated BOOLEAN DEFAULT FALSE,
    proration_date TIMESTAMP WITH TIME ZONE,
    
    -- Metadata
    plan_id UUID,
    plan_name VARCHAR(256),
    
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Invoice Line Items indexes
CREATE INDEX IF NOT EXISTS idx_invoice_items_invoice ON invoice_line_items(invoice_id);
CREATE INDEX IF NOT EXISTS idx_invoice_items_subscription_item ON invoice_line_items(subscription_item_id);

-- Invoice Line Items constraints
ALTER TABLE invoice_line_items
    ADD CONSTRAINT IF NOT EXISTS chk_invoice_items_quantity_non_negative CHECK (quantity >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_invoice_items_unit_amount_non_negative CHECK (unit_amount >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_invoice_items_amount_non_negative CHECK (amount >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS fk_invoice_items_invoice
        FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE CASCADE,
    ADD CONSTRAINT IF NOT EXISTS fk_invoice_items_subscription_item
        FOREIGN KEY (subscription_item_id) REFERENCES subscription_items(id) ON DELETE SET NULL,
    ADD CONSTRAINT IF NOT EXISTS fk_invoice_items_plan
        FOREIGN KEY (plan_id) REFERENCES billing_plans(id) ON DELETE SET NULL;

-- ============================================================================
-- Payments Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS payments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    payment_number VARCHAR(100) NOT NULL UNIQUE,
    invoice_id UUID,
    organization_id UUID NOT NULL,
    subscription_id UUID,
    
    -- Payment status
    status VARCHAR(50) NOT NULL DEFAULT 'pending', -- pending, processing, succeeded, failed, canceled, disputed, refunded
    
    -- Amount
    amount DECIMAL(18,8) NOT NULL,
    currency VARCHAR(3) DEFAULT 'USD',
    amount_refunded DECIMAL(18,8) DEFAULT 0.0,
    
    -- Payment method
    payment_method_type VARCHAR(50) NOT NULL, -- card, ach_debit, wire_transfer, paypal, etc.
    payment_method_details JSONB,
    
    -- Card-specific (if applicable)
    card_brand VARCHAR(50),
    card_last4 VARCHAR(4),
    card_exp_month INTEGER,
    card_exp_year INTEGER,
    
    -- Bank transfer (if applicable)
    bank_name VARCHAR(256),
    bank_account_last4 VARCHAR(4),
    
    -- External processor info
    processor VARCHAR(50) DEFAULT 'stripe', -- stripe, paypal, adyen, etc.
    processor_payment_id VARCHAR(256),
    processor_charge_id VARCHAR(256),
    processor_refund_id VARCHAR(256),
    
    -- Receipt
    receipt_url VARCHAR(512),
    receipt_email VARCHAR(256),
    
    -- Failure info
    failure_code VARCHAR(100),
    failure_message TEXT,
    
    -- Dispute info
    disputed BOOLEAN DEFAULT FALSE,
    dispute_id VARCHAR(256),
    dispute_status VARCHAR(50),
    dispute_reason VARCHAR(100),
    
    -- Refund info
    refunded BOOLEAN DEFAULT FALSE,
    refund_reason TEXT,
    
    -- Timestamps
    captured_at TIMESTAMP WITH TIME ZONE,
    failed_at TIMESTAMP WITH TIME ZONE,
    canceled_at TIMESTAMP WITH TIME ZONE,
    
    -- Legacy mapping
    legacy_payment_id VARCHAR(256),
    
    metadata JSONB DEFAULT '{}',
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Payments indexes
CREATE INDEX IF NOT EXISTS idx_payments_org ON payments(organization_id);
CREATE INDEX IF NOT EXISTS idx_payments_invoice ON payments(invoice_id);
CREATE INDEX IF NOT EXISTS idx_payments_subscription ON payments(subscription_id);
CREATE INDEX IF NOT EXISTS idx_payments_status ON payments(status);
CREATE INDEX IF NOT EXISTS idx_payments_number ON payments(payment_number);
CREATE INDEX IF NOT EXISTS idx_payments_processor_id ON payments(processor_payment_id);
CREATE INDEX IF NOT EXISTS idx_payments_created ON payments(created_at);

-- Payments constraints
ALTER TABLE payments
    ADD CONSTRAINT IF NOT EXISTS chk_payments_status CHECK (status IN ('pending', 'processing', 'succeeded', 'failed', 'canceled', 'disputed', 'refunded', 'partially_refunded')),
    ADD CONSTRAINT IF NOT EXISTS chk_payments_amount_non_negative CHECK (amount >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_payments_amount_refunded_non_negative CHECK (amount_refunded >= 0.0 AND amount_refunded <= amount),
    ADD CONSTRAINT IF NOT EXISTS chk_payments_method_type CHECK (payment_method_type IN ('card', 'ach_debit', 'ach_credit', 'wire_transfer', 'paypal', 'crypto', 'check', 'cash', 'other')),
    ADD CONSTRAINT IF NOT EXISTS fk_payments_invoice
        FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE SET NULL,
    ADD CONSTRAINT IF NOT EXISTS fk_payments_subscription
        FOREIGN KEY (subscription_id) REFERENCES subscriptions(id) ON DELETE SET NULL;

-- ============================================================================
-- Credit Notes Table (for refunds and adjustments)
-- ============================================================================
CREATE TABLE IF NOT EXISTS credit_notes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    credit_note_number VARCHAR(100) NOT NULL UNIQUE,
    invoice_id UUID NOT NULL,
    organization_id UUID NOT NULL,
    
    -- Amounts
    amount DECIMAL(18,8) NOT NULL,
    currency VARCHAR(3) DEFAULT 'USD',
    amount_refunded DECIMAL(18,8) DEFAULT 0.0,
    balance DECIMAL(18,8) NOT NULL, -- Remaining credit
    
    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'issued', -- issued, applied, void
    
    -- Reason
    reason_code VARCHAR(50), -- duplicate, fraudulent, customer_request, etc.
    reason_description TEXT,
    
    -- PDF/document
    pdf_url VARCHAR(512),
    
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    voided_at TIMESTAMP WITH TIME ZONE
);

-- Credit Notes indexes
CREATE INDEX IF NOT EXISTS idx_credit_notes_org ON credit_notes(organization_id);
CREATE INDEX IF NOT EXISTS idx_credit_notes_invoice ON credit_notes(invoice_id);
CREATE INDEX IF NOT EXISTS idx_credit_notes_status ON credit_notes(status);
CREATE INDEX IF NOT EXISTS idx_credit_notes_number ON credit_notes(credit_note_number);

-- Credit Notes constraints
ALTER TABLE credit_notes
    ADD CONSTRAINT IF NOT EXISTS chk_credit_notes_status CHECK (status IN ('issued', 'applied', 'partially_applied', 'void')),
    ADD CONSTRAINT IF NOT EXISTS chk_credit_notes_amount_non_negative CHECK (amount >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS chk_credit_notes_reason_code CHECK (reason_code IN ('duplicate', 'fraudulent', 'customer_request', 'product_unsatisfactory', 'service_disruption', 'billing_error', 'other')),
    ADD CONSTRAINT IF NOT EXISTS fk_credit_notes_invoice
        FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE RESTRICT;

-- ============================================================================
-- Usage Records Table (for metered billing)
-- ============================================================================
CREATE TABLE IF NOT EXISTS usage_records (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL,
    subscription_id UUID,
    subscription_item_id UUID,
    
    -- Usage data
    quantity DECIMAL(18,8) NOT NULL,
    usage_type VARCHAR(100) NOT NULL, -- tokens, requests, storage, bandwidth, etc.
    unit VARCHAR(50), -- tokens, GB, MB, hours, etc.
    
    -- Timestamp
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    
    -- Source
    resource_type VARCHAR(100), -- model, endpoint, etc.
    resource_id VARCHAR(256),
    request_id VARCHAR(256),
    
    -- Whether this has been billed
    billed BOOLEAN DEFAULT FALSE,
    invoice_id UUID,
    
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Usage Records indexes
CREATE INDEX IF NOT EXISTS idx_usage_records_org ON usage_records(organization_id);
CREATE INDEX IF NOT EXISTS idx_usage_records_subscription ON usage_records(subscription_id);
CREATE INDEX IF NOT EXISTS idx_usage_records_timestamp ON usage_records(timestamp);
CREATE INDEX IF NOT EXISTS idx_usage_records_type ON usage_records(usage_type);
CREATE INDEX IF NOT EXISTS idx_usage_records_billed ON usage_records(billed) WHERE billed = FALSE;
CREATE INDEX IF NOT EXISTS idx_usage_records_invoice ON usage_records(invoice_id);

-- Usage Records constraints
ALTER TABLE usage_records
    ADD CONSTRAINT IF NOT EXISTS chk_usage_records_quantity_non_negative CHECK (quantity >= 0.0),
    ADD CONSTRAINT IF NOT EXISTS fk_usage_records_subscription
        FOREIGN KEY (subscription_id) REFERENCES subscriptions(id) ON DELETE SET NULL,
    ADD CONSTRAINT IF NOT EXISTS fk_usage_records_subscription_item
        FOREIGN KEY (subscription_item_id) REFERENCES subscription_items(id) ON DELETE SET NULL,
    ADD CONSTRAINT IF NOT EXISTS fk_usage_records_invoice
        FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE SET NULL;

-- ============================================================================
-- Triggers
-- ============================================================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_billing_plans_updated_at') THEN
        CREATE TRIGGER trg_billing_plans_updated_at
        BEFORE UPDATE ON billing_plans
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_subscriptions_updated_at') THEN
        CREATE TRIGGER trg_subscriptions_updated_at
        BEFORE UPDATE ON subscriptions
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_subscription_items_updated_at') THEN
        CREATE TRIGGER trg_subscription_items_updated_at
        BEFORE UPDATE ON subscription_items
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_invoices_updated_at') THEN
        CREATE TRIGGER trg_invoices_updated_at
        BEFORE UPDATE ON invoices
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_payments_updated_at') THEN
        CREATE TRIGGER trg_payments_updated_at
        BEFORE UPDATE ON payments
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_credit_notes_updated_at') THEN
        CREATE TRIGGER trg_credit_notes_updated_at
        BEFORE UPDATE ON credit_notes
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;
END $$;

-- ============================================================================
-- Migration Completion Log
-- ============================================================================
INSERT INTO schema_migrations (version, applied_at, description)
VALUES ('005', NOW(), 'BillingSchema: billing_plans, subscriptions, subscription_items, invoices, invoice_line_items, payments, credit_notes, usage_records')
ON CONFLICT (version) DO UPDATE SET
    applied_at = EXCLUDED.applied_at,
    description = EXCLUDED.description;
