using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaxis.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMultiTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    primary_region = table.Column<string>(type: "text", nullable: false),
                    AvailableRegions = table.Column<string[]>(type: "text[]", nullable: true),
                    tier = table.Column<string>(type: "text", nullable: true),
                    billing_currency = table.Column<string>(type: "text", nullable: true),
                    credit_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    CreditCurrency = table.Column<string>(type: "text", nullable: true),
                    SubscriptionStatus = table.Column<string>(type: "text", nullable: true),
                    SubscriptionStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsTrial = table.Column<bool>(type: "boolean", nullable: false),
                    TrialStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxTeams = table.Column<int>(type: "integer", nullable: true),
                    MaxUsersPerTeam = table.Column<int>(type: "integer", nullable: true),
                    MaxKeysPerUser = table.Column<int>(type: "integer", nullable: true),
                    MaxConcurrentRequests = table.Column<int>(type: "integer", nullable: true),
                    MonthlyRequestLimit = table.Column<long>(type: "bigint", nullable: true),
                    MonthlyTokenLimit = table.Column<long>(type: "bigint", nullable: true),
                    DataRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    RequireSso = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedEmailDomains = table.Column<string[]>(type: "text[]", nullable: true),
                    privacy_consent = table.Column<string>(type: "jsonb", nullable: true),
                    TermsAcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    monthly_price_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    yearly_price_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    limits_config = table.Column<string>(type: "jsonb", nullable: true),
                    features = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "credit_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount_usd = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    balance_before_usd = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    balance_after_usd = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    initiated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_credit_transactions_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_amount_usd = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    total_amount_billing_currency = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    billing_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoices_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_backup_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    strategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    schedule_hour = table.Column<int>(type: "integer", nullable: false),
                    TargetRegions = table.Column<string[]>(type: "text[]", nullable: true),
                    enable_encryption = table.Column<bool>(type: "boolean", nullable: false),
                    encryption_key_id = table.Column<string>(type: "text", nullable: true),
                    retention_days = table.Column<int>(type: "integer", nullable: false),
                    enable_postgres_backup = table.Column<bool>(type: "boolean", nullable: false),
                    enable_redis_backup = table.Column<bool>(type: "boolean", nullable: false),
                    enable_qdrant_backup = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_backup_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_backup_status = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_backup_config", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_backup_config_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    monthly_budget = table.Column<decimal>(type: "numeric", nullable: true),
                    budget_alert_threshold = table.Column<decimal>(type: "numeric", nullable: false),
                    AllowedModels = table.Column<string[]>(type: "text[]", nullable: true),
                    BlockedModels = table.Column<string[]>(type: "text[]", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email_verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    data_residency_region = table.Column<string>(type: "text", nullable: false),
                    created_in_region = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    role = table.Column<string>(type: "text", nullable: true),
                    privacy_consent = table.Column<string>(type: "jsonb", nullable: true),
                    cross_border_consent_given = table.Column<bool>(type: "boolean", nullable: false),
                    cross_border_consent_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cross_border_consent_version = table.Column<string>(type: "text", nullable: true),
                    mfa_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    mfa_secret = table.Column<string>(type: "text", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    resource_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    integrity_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    previous_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "team_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: true),
                    InviterId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_memberships_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_memberships_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_memberships_users_InviterId",
                        column: x => x.InviterId,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_team_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "virtual_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<string>(type: "text", nullable: true),
                    max_budget = table.Column<decimal>(type: "numeric", nullable: true),
                    current_spend = table.Column<decimal>(type: "numeric", nullable: false),
                    rpm_limit = table.Column<int>(type: "integer", nullable: true),
                    tpm_limit = table.Column<int>(type: "integer", nullable: true),
                    AllowedModels = table.Column<string[]>(type: "text[]", nullable: true),
                    BlockedModels = table.Column<string[]>(type: "text[]", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Tags = table.Column<string[]>(type: "text[]", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    user_region = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_virtual_keys", x => x.id);
                    table.ForeignKey(
                        name: "FK_virtual_keys_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_virtual_keys_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_virtual_keys_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_virtual_keys_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    virtual_key_id = table.Column<Guid>(type: "uuid", nullable: true),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_region = table.Column<string>(type: "text", nullable: true),
                    processed_region = table.Column<string>(type: "text", nullable: true),
                    stored_region = table.Column<string>(type: "text", nullable: true),
                    cross_border_transfer = table.Column<bool>(type: "boolean", nullable: false),
                    transfer_legal_basis = table.Column<string>(type: "text", nullable: true),
                    transfer_purpose = table.Column<string>(type: "text", nullable: true),
                    transfer_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    input_tokens = table.Column<int>(type: "integer", nullable: false),
                    output_tokens = table.Column<int>(type: "integer", nullable: false),
                    cost = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    queue_time_ms = table.Column<int>(type: "integer", nullable: false),
                    request_size_bytes = table.Column<int>(type: "integer", nullable: false),
                    response_size_bytes = table.Column<int>(type: "integer", nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    client_ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    request_headers = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_requests_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_requests_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_requests_virtual_keys_virtual_key_id",
                        column: x => x.virtual_key_id,
                        principalTable: "virtual_keys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "spend_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    virtual_key_id = table.Column<Guid>(type: "uuid", nullable: true),
                    request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_usd = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tokens = table.Column<int>(type: "integer", nullable: false),
                    region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spend_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_spend_logs_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_spend_logs_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_spend_logs_virtual_keys_virtual_key_id",
                        column: x => x.virtual_key_id,
                        principalTable: "virtual_keys",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_event_category",
                table: "audit_logs",
                column: "event_category");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_event_type",
                table: "audit_logs",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_organization_id",
                table: "audit_logs",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_organization_id_timestamp",
                table: "audit_logs",
                columns: new[] { "organization_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_timestamp",
                table: "audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_transactions_created_at",
                table: "credit_transactions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_credit_transactions_organization_id",
                table: "credit_transactions",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_transactions_organization_id_created_at",
                table: "credit_transactions",
                columns: new[] { "organization_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_credit_transactions_transaction_type",
                table: "credit_transactions",
                column: "transaction_type");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_invoice_number",
                table: "invoices",
                column: "invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_organization_id",
                table: "invoices",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_organization_id_period_start_period_end",
                table: "invoices",
                columns: new[] { "organization_id", "period_start", "period_end" });

            migrationBuilder.CreateIndex(
                name: "IX_invoices_status",
                table: "invoices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_organization_backup_config_organization_id",
                table: "organization_backup_config",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_organization_backup_config_organization_id_is_active",
                table: "organization_backup_config",
                columns: new[] { "organization_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_organizations_primary_region",
                table: "organizations",
                column: "primary_region");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_slug",
                table: "organizations",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organizations_tier",
                table: "organizations",
                column: "tier");

            migrationBuilder.CreateIndex(
                name: "IX_requests_created_at",
                table: "requests",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_requests_cross_border_transfer",
                table: "requests",
                column: "cross_border_transfer");

            migrationBuilder.CreateIndex(
                name: "IX_requests_organization_id",
                table: "requests",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_requests_organization_id_created_at",
                table: "requests",
                columns: new[] { "organization_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_requests_request_id",
                table: "requests",
                column: "request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_requests_team_id",
                table: "requests",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_requests_user_id",
                table: "requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_requests_virtual_key_id",
                table: "requests",
                column: "virtual_key_id");

            migrationBuilder.CreateIndex(
                name: "IX_spend_logs_created_at",
                table: "spend_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_spend_logs_organization_id",
                table: "spend_logs",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_spend_logs_organization_id_created_at",
                table: "spend_logs",
                columns: new[] { "organization_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_spend_logs_team_id",
                table: "spend_logs",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_spend_logs_virtual_key_id",
                table: "spend_logs",
                column: "virtual_key_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_is_active",
                table: "subscription_plans",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_slug",
                table: "subscription_plans",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_memberships_InviterId",
                table: "team_memberships",
                column: "InviterId");

            migrationBuilder.CreateIndex(
                name: "IX_team_memberships_organization_id_user_id",
                table: "team_memberships",
                columns: new[] { "organization_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_team_memberships_team_id",
                table: "team_memberships",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_memberships_user_id_team_id",
                table: "team_memberships",
                columns: new[] { "user_id", "team_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_organization_id_slug",
                table: "teams",
                columns: new[] { "organization_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_data_residency_region",
                table: "users",
                column: "data_residency_region");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_organization_id",
                table: "users",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_virtual_keys_created_by",
                table: "virtual_keys",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_virtual_keys_key_hash",
                table: "virtual_keys",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_virtual_keys_organization_id_team_id",
                table: "virtual_keys",
                columns: new[] { "organization_id", "team_id" });

            migrationBuilder.CreateIndex(
                name: "IX_virtual_keys_team_id",
                table: "virtual_keys",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_virtual_keys_UserId",
                table: "virtual_keys",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "credit_transactions");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "organization_backup_config");

            migrationBuilder.DropTable(
                name: "requests");

            migrationBuilder.DropTable(
                name: "spend_logs");

            migrationBuilder.DropTable(
                name: "subscription_plans");

            migrationBuilder.DropTable(
                name: "team_memberships");

            migrationBuilder.DropTable(
                name: "virtual_keys");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "organizations");
        }
    }
}
