-- Rollback: rollback_005.sql
-- Description: Rollback for 005_BillingSchema
-- Idempotent: Yes
-- Created: 2026-03-04

DO $$
BEGIN
    RAISE NOTICE 'Rolling back 005_BillingSchema...';
    
    -- Drop triggers
    DROP TRIGGER IF EXISTS trg_billing_plans_updated_at ON billing_plans;
    DROP TRIGGER IF EXISTS trg_subscriptions_updated_at ON subscriptions;
    DROP TRIGGER IF EXISTS trg_subscription_items_updated_at ON subscription_items;
    DROP TRIGGER IF EXISTS trg_invoices_updated_at ON invoices;
    DROP TRIGGER IF EXISTS trg_payments_updated_at ON payments;
    DROP TRIGGER IF EXISTS trg_credit_notes_updated_at ON credit_notes;
    
    -- Drop tables (order matters for FKs)
    DROP TABLE IF EXISTS usage_records CASCADE;
    DROP TABLE IF EXISTS credit_notes CASCADE;
    DROP TABLE IF EXISTS payments CASCADE;
    DROP TABLE IF EXISTS invoice_line_items CASCADE;
    DROP TABLE IF EXISTS invoices CASCADE;
    DROP TABLE IF EXISTS subscription_items CASCADE;
    DROP TABLE IF EXISTS subscriptions CASCADE;
    DROP TABLE IF EXISTS billing_plans CASCADE;
    
    DELETE FROM schema_migrations WHERE version = '005';
    RAISE NOTICE 'Rollback 005 complete';
END $$;
