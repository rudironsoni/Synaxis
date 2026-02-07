// <copyright file="20260204151324_AddEnterpriseMultiTenantSchema.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseMultiTenantSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Tenants_TenantId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AuditLogs");

            migrationBuilder.EnsureSchema(
                name: "operations");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "AuditLogs",
                newSchema: "audit");

            migrationBuilder.RenameColumn(
                name: "PayloadJson",
                schema: "audit",
                table: "AuditLogs",
                newName: "PreviousValues");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAt",
                table: "ApiKeys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                schema: "audit",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                schema: "audit",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                schema: "audit",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                schema: "audit",
                table: "AuditLogs",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewValues",
                schema: "audit",
                table: "AuditLogs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "audit",
                table: "AuditLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PartitionDate",
                schema: "audit",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                schema: "audit",
                table: "AuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    KeyPrefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Scopes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RateLimitRpm = table.Column<int>(type: "integer", nullable: true),
                    RateLimitTpm = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys1", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationModels",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InputCostPer1MTokens = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: true),
                    OutputCostPer1MTokens = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: true),
                    CustomAlias = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationProviders",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKeyEncrypted = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CustomEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InputCostPer1MTokens = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: true),
                    OutputCostPer1MTokens = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: true),
                    SupportsStreaming = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsTools = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsVision = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    RateLimitRpm = table.Column<int>(type: "integer", nullable: true),
                    RateLimitTpm = table.Column<int>(type: "integer", nullable: true),
                    HealthCheckEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TaxId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LegalAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PrimaryContactEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    BillingEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    SupportEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompanySize = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PlanTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequireMfa = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BaseEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DefaultApiKeyEnvironmentVariable = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SupportsStreaming = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsTools = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsVision = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultInputCostPer1MTokens = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: true),
                    DefaultOutputCostPer1MTokens = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: true),
                    IsFreeTier = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoutingStrategies",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StrategyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PrioritizeFreeProviders = table.Column<bool>(type: "boolean", nullable: false),
                    MaxCostPer1MTokens = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: true),
                    FallbackToPaid = table.Column<bool>(type: "boolean", nullable: false),
                    MaxLatencyMs = table.Column<int>(type: "integer", nullable: true),
                    RequireStreaming = table.Column<bool>(type: "boolean", nullable: false),
                    MinHealthScore = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingStrategies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "text", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users1", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProviderHealthStatuses",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsHealthy = table.Column<bool>(type: "boolean", nullable: false),
                    HealthScore = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    LastCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSuccessAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFailureAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                    LastErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AverageLatencyMs = table.Column<int>(type: "integer", nullable: true),
                    SuccessRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    IsInCooldown = table.Column<bool>(type: "boolean", nullable: false),
                    CooldownUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderHealthStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderHealthStatuses_OrganizationProviders_OrganizationPr~",
                        column: x => x.OrganizationProviderId,
                        principalSchema: "operations",
                        principalTable: "OrganizationProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RateLimitRpm = table.Column<int>(type: "integer", nullable: true),
                    RateLimitTpm = table.Column<int>(type: "integer", nullable: true),
                    AllowAutoOptimization = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDefaultGroup = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "identity",
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSettings",
                schema: "identity",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    JwtTokenLifetimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaxRequestBodySizeBytes = table.Column<int>(type: "integer", nullable: false),
                    DefaultRateLimitRpm = table.Column<int>(type: "integer", nullable: false),
                    DefaultRateLimitTpm = table.Column<int>(type: "integer", nullable: false),
                    AllowAutoOptimization = table.Column<bool>(type: "boolean", nullable: false),
                    AllowCustomProviders = table.Column<bool>(type: "boolean", nullable: false),
                    AllowAuditLogExport = table.Column<bool>(type: "boolean", nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: false),
                    MaxGroups = table.Column<int>(type: "integer", nullable: false),
                    MonthlyTokenQuota = table.Column<long>(type: "bigint", nullable: true),
                    AuditLogRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSettings", x => x.OrganizationId);
                    table.ForeignKey(
                        name: "FK_OrganizationSettings_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "identity",
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedName = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "identity",
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Models",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContextWindowTokens = table.Column<int>(type: "integer", nullable: true),
                    MaxOutputTokens = table.Column<int>(type: "integer", nullable: true),
                    SupportsStreaming = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsTools = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsVision = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Models", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Models_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "platform",
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGroupMemberships",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroupMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroupMemberships_Groups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "identity",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroupMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserOrganizationMemberships",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PrimaryGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    RateLimitRpm = table.Column<int>(type: "integer", nullable: true),
                    RateLimitTpm = table.Column<int>(type: "integer", nullable: true),
                    AllowAutoOptimization = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOrganizationMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOrganizationMemberships_Groups_PrimaryGroupId",
                        column: x => x.PrimaryGroupId,
                        principalSchema: "identity",
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserOrganizationMemberships_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "identity",
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserOrganizationMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "identity",
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                schema: "audit",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                schema: "audit",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OrganizationId",
                schema: "audit",
                table: "AuditLogs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PartitionDate",
                schema: "audit",
                table: "AuditLogs",
                column: "PartitionDate");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_IsActive",
                schema: "operations",
                table: "ApiKeys",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyHash",
                schema: "operations",
                table: "ApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_OrganizationId",
                schema: "operations",
                table: "ApiKeys",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OrganizationId_Slug",
                schema: "identity",
                table: "Groups",
                columns: new[] { "OrganizationId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Status",
                schema: "identity",
                table: "Groups",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Models_IsActive",
                schema: "platform",
                table: "Models",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Models_ProviderId",
                schema: "platform",
                table: "Models",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Models_ProviderId_CanonicalId",
                schema: "platform",
                table: "Models",
                columns: new[] { "ProviderId", "CanonicalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationModels_IsEnabled",
                schema: "operations",
                table: "OrganizationModels",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationModels_ModelId",
                schema: "operations",
                table: "OrganizationModels",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationModels_OrganizationId_ModelId",
                schema: "operations",
                table: "OrganizationModels",
                columns: new[] { "OrganizationId", "ModelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationProviders_IsEnabled",
                schema: "operations",
                table: "OrganizationProviders",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationProviders_OrganizationId_ProviderId",
                schema: "operations",
                table: "OrganizationProviders",
                columns: new[] { "OrganizationId", "ProviderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationProviders_ProviderId",
                schema: "operations",
                table: "OrganizationProviders",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CreatedAt",
                schema: "identity",
                table: "Organizations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Slug",
                schema: "identity",
                table: "Organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Status",
                schema: "identity",
                table: "Organizations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderHealthStatuses_LastCheckedAt",
                schema: "operations",
                table: "ProviderHealthStatuses",
                column: "LastCheckedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderHealthStatuses_OrganizationId_IsHealthy",
                schema: "operations",
                table: "ProviderHealthStatuses",
                columns: new[] { "OrganizationId", "IsHealthy" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderHealthStatuses_OrganizationProviderId",
                schema: "operations",
                table: "ProviderHealthStatuses",
                column: "OrganizationProviderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Providers_IsActive",
                schema: "platform",
                table: "Providers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Key",
                schema: "platform",
                table: "Providers",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_IsSystemRole",
                schema: "identity",
                table: "Roles",
                column: "IsSystemRole");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_OrganizationId_Name",
                schema: "identity",
                table: "Roles",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"OrganizationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingStrategies_IsActive",
                schema: "operations",
                table: "RoutingStrategies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingStrategies_OrganizationId",
                schema: "operations",
                table: "RoutingStrategies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutingStrategies_OrganizationId_IsDefault",
                schema: "operations",
                table: "RoutingStrategies",
                columns: new[] { "OrganizationId", "IsDefault" },
                unique: true,
                filter: "\"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupMemberships_GroupId",
                schema: "identity",
                table: "UserGroupMemberships",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupMemberships_IsPrimary",
                schema: "identity",
                table: "UserGroupMemberships",
                column: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupMemberships_UserId_GroupId",
                schema: "identity",
                table: "UserGroupMemberships",
                columns: new[] { "UserId", "GroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizationMemberships_OrganizationId",
                schema: "identity",
                table: "UserOrganizationMemberships",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizationMemberships_PrimaryGroupId",
                schema: "identity",
                table: "UserOrganizationMemberships",
                column: "PrimaryGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizationMemberships_Status",
                schema: "identity",
                table: "UserOrganizationMemberships",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizationMemberships_UserId_OrganizationId",
                schema: "identity",
                table: "UserOrganizationMemberships",
                columns: new[] { "UserId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_OrganizationId",
                schema: "identity",
                table: "UserRoles",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "identity",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "identity",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status",
                schema: "identity",
                table: "Users",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "Models",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "OrganizationModels",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "OrganizationSettings",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "ProviderHealthStatuses",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "RoutingStrategies",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "UserGroupMemberships",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserOrganizationMemberships",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Providers",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "OrganizationProviders",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "Groups",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Organizations",
                schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Action",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_CreatedAt",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_OrganizationId",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_PartitionDate",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "EntityId",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "EntityType",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "NewValues",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "PartitionDate",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                schema: "audit",
                newName: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "PreviousValues",
                table: "AuditLogs",
                newName: "PayloadJson");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AuditLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Tenants_TenantId",
                table: "AuditLogs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
