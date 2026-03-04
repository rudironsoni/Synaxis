-- =============================================================================
-- Data Transformation: Migrate Billing Records to Aggregate Structure
-- Description: Transforms billing records to new aggregate-based structure
-- Created: 2026-03-04
-- Idempotent: Yes
-- =============================================================================

-- =============================================================================
-- PRE-MIGRATION BACKUP
-- =============================================================================

-- Create backup tables
CREATE TABLE IF NOT EXISTS credit_transactions_backup AS
SELECT * FROM credit_transactions WHERE 1=0;

CREATE TABLE IF NOT EXISTS invoices_backup AS
SELECT * FROM invoices WHERE 1=0;

-- Backup data if not already backed up
INSERT INTO credit_transactions_backup
SELECT * FROM credit_transactions
WHERE NOT EXISTS (SELECT 1 FROM credit_transactions_backup LIMIT 1);

INSERT INTO invoices_backup
SELECT * FROM invoices
WHERE NOT EXISTS (SELECT 1 FROM invoices_backup LIMIT 1);

-- =============================================================================
-- MIGRATION: BILLING AGGREGATES
-- =============================================================================

-- Create BillingAggregate table
CREATE TABLE IF NOT EXISTS billing_aggregates (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL UNIQUE REFERENCES organizations(id) ON DELETE CASCADE,
    aggregate_version BIGINT NOT NULL DEFAULT 0,
    current_balance_usd DECIMAL(18, 8) NOT NULL DEFAULT 0,
    lifetime_credits_usd DECIMAL(18, 8) NOT NULL DEFAULT 0,
    lifetime_charges_usd DECIMAL(18, 8) NOT NULL DEFAULT 0,
    pending_charges_usd DECIMAL(18, 8) NOT NULL DEFAULT 0,
    last_invoice_date DATE,
    last_invoice_amount DECIMAL(18, 8),
    billing_cycle_start DATE,
    billing_cycle_end DATE,
    auto_recharge_enabled BOOLEAN NOT NULL DEFAULT false,
    auto_recharge_threshold DECIMAL(18, 8),
    auto_recharge_amount DECIMAL(18, 8),
    payment_method_id TEXT,
    tax_exempt BOOLEAN NOT NULL DEFAULT false,
    tax_id TEXT,
    billing_address JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_billing_aggregates_org ON billing_aggregates(organization_id);

-- Migrate organization billing data
INSERT INTO billing_aggregates (
    id,
    organization_id,
    current_balance_usd,
    aggregate_version,
    created_at,
    updated_at
)
SELECT
    uuid_generate_v4(),
    o.id,
    o.credit_balance,
    1,
    o.created_at,
    o.updated_at
FROM organizations o
LEFT JOIN billing_aggregates ba ON ba.organization_id = o.id
WHERE ba.id IS NULL;

-- Update aggregates with transaction totals
UPDATE billing_aggregates ba
SET
    lifetime_credits_usd = COALESCE((
        SELECT SUM(ct.amount_usd)
        FROM credit_transactions ct
        WHERE ct.organization_id = ba.organization_id
          AND ct.transaction_type IN ('credit_purchase', 'credit_grant', 'refund')
    ), 0),
    lifetime_charges_usd = COALESCE((
        SELECT SUM(ABS(ct.amount_usd))
        FROM credit_transactions ct
        WHERE ct.organization_id = ba.organization_id
          AND ct.transaction_type IN ('usage_charge', 'subscription_charge')
    ), 0),
    updated_at = NOW();

-- =============================================================================
-- MIGRATION: SUBSCRIPTION RECORDS
-- =============================================================================

-- Create Subscriptions table for detailed subscription tracking
CREATE TABLE IF NOT EXISTS subscriptions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    plan_id UUID REFERENCES subscription_plans(id) ON DELETE RESTRICT,
    plan_slug VARCHAR(100) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'inactive',
    current_period_start DATE NOT NULL,
    current_period_end DATE NOT NULL,
    trial_start DATE,
    trial_end DATE,
    canceled_at TIMESTAMPTZ,
    cancel_at_period_end BOOLEAN NOT NULL DEFAULT false,
    quantity INTEGER NOT NULL DEFAULT 1,
    unit_price_usd DECIMAL(18, 2) NOT NULL DEFAULT 0,
    discount_percent DECIMAL(5, 2) DEFAULT 0,
    metadata JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_subscriptions_org ON subscriptions(organization_id);
CREATE INDEX IF NOT EXISTS idx_subscriptions_status ON subscriptions(status);
CREATE INDEX IF NOT EXISTS idx_subscriptions_period ON subscriptions(current_period_start, current_period_end);

-- Migrate subscription data from organizations
INSERT INTO subscriptions (
    id,
    organization_id,
    plan_slug,
    status,
    current_period_start,
    current_period_end,
    trial_start,
    trial_end,
    created_at,
    updated_at
)
SELECT
    uuid_generate_v4(),
    o.id,
    o.tier,
    CASE
        WHEN o.subscription_expires_at IS NOT NULL AND o.subscription_expires_at < NOW() THEN 'expired'
        WHEN o.is_trial = true AND o.trial_ends_at > NOW() THEN 'trialing'
        ELSE o.subscription_status
    END::varchar(50),
    COALESCE(o.subscription_started_at::date, o.created_at::date),
    COALESCE(o.subscription_expires_at::date, (o.created_at + INTERVAL '1 year')::date),
    o.trial_started_at::date,
    o.trial_ends_at::date,
    o.created_at,
    o.updated_at
FROM organizations o
LEFT JOIN subscriptions s ON s.organization_id = o.id
WHERE s.id IS NULL
  AND (o.subscription_status IS NOT NULL OR o.is_trial = true);

-- =============================================================================
-- MIGRATION: INVOICE LINE ITEMS
-- =============================================================================

-- Create InvoiceLineItems table for detailed invoice breakdown
CREATE TABLE IF NOT EXISTS invoice_line_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    line_item_type VARCHAR(50) NOT NULL, -- 'usage', 'subscription', 'credit', 'tax'
    description TEXT NOT NULL,
    quantity DECIMAL(18, 6) NOT NULL DEFAULT 1,
    unit_price_usd DECIMAL(18, 8) NOT NULL DEFAULT 0,
    amount_usd DECIMAL(18, 8) NOT NULL DEFAULT 0,
    metadata JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_invoice_line_items_invoice ON invoice_line_items(invoice_id);
CREATE INDEX IF NOT EXISTS idx_invoice_line_items_type ON invoice_line_items(line_item_type);

-- Generate line items from invoices
INSERT INTO invoice_line_items (
    invoice_id,
    line_item_type,
    description,
    quantity,
    amount_usd
)
SELECT
    i.id,
    'subscription',
    'Subscription charges for period ' || i.period_start::date || ' to ' || i.period_end::date,
    1,
    i.total_amount_usd
FROM invoices i
LEFT JOIN invoice_line_items ili ON ili.invoice_id = i.id
WHERE ili.id IS NULL;

-- =============================================================================
-- MIGRATION: USAGE RECORDS
-- =============================================================================

-- Create UsageRecords table for detailed usage tracking
CREATE TABLE IF NOT EXISTS usage_records (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    subscription_id UUID REFERENCES subscriptions(id) ON DELETE SET NULL,
    resource_type VARCHAR(50) NOT NULL, -- 'inference', 'storage', 'bandwidth'
    resource_id UUID,
    quantity DECIMAL(18, 6) NOT NULL,
    unit VARCHAR(20) NOT NULL, -- 'tokens', 'requests', 'gb', 'hours'
    cost_usd DECIMAL(18, 8) NOT NULL DEFAULT 0,
    model VARCHAR(100),
    provider VARCHAR(100),
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    invoice_id UUID REFERENCES invoices(id) ON DELETE SET NULL,
    metadata JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_usage_records_org ON usage_records(organization_id);
CREATE INDEX IF NOT EXISTS idx_usage_records_resource ON usage_records(resource_type);
CREATE INDEX IF NOT EXISTS idx_usage_records_timestamp ON usage_records(timestamp);
CREATE INDEX IF NOT EXISTS idx_usage_records_invoice ON usage_records(invoice_id);

-- Migrate spend logs to usage records
INSERT INTO usage_records (
    id,
    organization_id,
    resource_type,
    quantity,
    unit,
    cost_usd,
    model,
    provider,
    timestamp,
    created_at
)
SELECT
    uuid_generate_v4(),
    sl.organization_id,
    'inference',
    sl.tokens,
    'tokens',
    sl.amount_usd,
    sl.model,
    sl.provider,
    sl.created_at,
    sl.created_at
FROM spend_logs sl
LEFT JOIN usage_records ur ON ur.organization_id = sl.organization_id
    AND ur.timestamp = sl.created_at
    AND ur.quantity = sl.tokens
WHERE ur.id IS NULL;

-- =============================================================================
-- MIGRATION: PAYMENT METHODS
-- =============================================================================

-- Create PaymentMethods table
CREATE TABLE IF NOT EXISTS payment_methods (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    type VARCHAR(50) NOT NULL, -- 'card', 'bank_transfer', 'paypal'
    provider VARCHAR(50) NOT NULL,
    provider_payment_method_id TEXT NOT NULL,
    last_four TEXT,
    brand TEXT,
    expiry_month INTEGER,
    expiry_year INTEGER,
    is_default BOOLEAN NOT NULL DEFAULT false,
    is_active BOOLEAN NOT NULL DEFAULT true,
    billing_details JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_payment_methods_org ON payment_methods(organization_id);
CREATE INDEX IF NOT EXISTS idx_payment_methods_default ON payment_methods(organization_id, is_default) WHERE is_default = true;

-- =============================================================================
-- POST-MIGRATION VALIDATION
-- =============================================================================

DO $$
DECLARE
    v_total_orgs INTEGER;
    v_billing_aggregates INTEGER;
    v_subscriptions INTEGER;
    v_line_items INTEGER;
    v_usage_records INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_total_orgs FROM organizations;
    SELECT COUNT(*) INTO v_billing_aggregates FROM billing_aggregates;
    SELECT COUNT(*) INTO v_subscriptions FROM subscriptions;
    SELECT COUNT(*) INTO v_line_items FROM invoice_line_items;
    SELECT COUNT(*) INTO v_usage_records FROM usage_records;

    RAISE NOTICE 'Billing Migration Summary:';
    RAISE NOTICE '  - Total organizations: %', v_total_orgs;
    RAISE NOTICE '  - Billing aggregates created: %', v_billing_aggregates;
    RAISE NOTICE '  - Subscriptions created: %', v_subscriptions;
    RAISE NOTICE '  - Invoice line items created: %', v_line_items;
    RAISE NOTICE '  - Usage records created: %', v_usage_records;

    -- Verify all organizations have billing aggregates
    IF v_billing_aggregates != v_total_orgs THEN
        RAISE WARNING 'Mismatch: % organizations but % billing aggregates', v_total_orgs, v_billing_aggregates;
    END IF;
END $$;

-- =============================================================================
-- DATA INTEGRITY CHECKS
-- =============================================================================

DO $$
DECLARE
    v_orphaned INTEGER;
    v_negative_balance INTEGER;
BEGIN
    -- Check for orphaned billing aggregates
    SELECT COUNT(*) INTO v_orphaned
    FROM billing_aggregates ba
    WHERE NOT EXISTS (SELECT 1 FROM organizations o WHERE o.id = ba.organization_id);

    IF v_orphaned > 0 THEN
        RAISE WARNING 'Found % orphaned billing aggregates', v_orphaned;
    END IF;

    -- Check for subscriptions without valid organizations
    SELECT COUNT(*) INTO v_orphaned
    FROM subscriptions s
    WHERE NOT EXISTS (SELECT 1 FROM organizations o WHERE o.id = s.organization_id);

    IF v_orphaned > 0 THEN
        RAISE WARNING 'Found % subscriptions without valid organizations', v_orphaned;
    END IF;

    -- Check for negative balances (data quality issue)
    SELECT COUNT(*) INTO v_negative_balance
    FROM billing_aggregates
    WHERE current_balance_usd < 0;

    IF v_negative_balance > 0 THEN
        RAISE WARNING 'Found % billing aggregates with negative balance', v_negative_balance;
    END IF;

    -- Check invoice totals match line items
    SELECT COUNT(*) INTO v_orphaned
    FROM invoices i
    WHERE NOT EXISTS (
        SELECT 1 FROM invoice_line_items ili
        WHERE ili.invoice_id = i.id
    );

    IF v_orphaned > 0 THEN
        RAISE WARNING 'Found % invoices without line items', v_orphaned;
    END IF;
END $$;

-- =============================================================================
-- MIGRATION METADATA
-- =============================================================================

-- Record this transformation
INSERT INTO __migrations_history (migration_id, migration_name, checksum, is_idempotent)
VALUES ('DT003', 'MigrateBillingToAggregateStructure', MD5(pg_read_file('/scripts/data-transform/003_MigrateBilling.sql')::text), true)
ON CONFLICT (migration_id) DO NOTHING;

-- =============================================================================
-- CLEANUP (Optional - run after verification)
-- =============================================================================

-- Note: Uncomment below only after full verification
-- DROP TABLE IF EXISTS credit_transactions_backup;
-- DROP TABLE IF EXISTS invoices_backup;
