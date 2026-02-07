// <copyright file="20260129134724_AddDynamicModelRegistry.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicModelRegistry : Migration
    {
        /// <inheritdoc />
#pragma warning disable MA0051 // Method is too long - Migrations are inherently complex
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalModels",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Family = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContextWindow = table.Column<int>(type: "integer", nullable: false),
                    MaxOutputTokens = table.Column<int>(type: "integer", nullable: false),
                    InputPrice = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: false),
                    OutputPrice = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: false),
                    IsOpenWeights = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsTools = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsReasoning = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsVision = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsAudio = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsStructuredOutput = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProviderModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GlobalModelId = table.Column<string>(type: "text", nullable: false),
                    ProviderSpecificId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    OverrideInputPrice = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: true),
                    OverrideOutputPrice = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: true),
                    RateLimitRPM = table.Column<int>(type: "integer", nullable: true),
                    RateLimitTPM = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderModels_GlobalModels_GlobalModelId",
                        column: x => x.GlobalModelId,
                        principalTable: "GlobalModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantModelLimits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GlobalModelId = table.Column<string>(type: "text", nullable: false),
                    AllowedRPM = table.Column<int>(type: "integer", nullable: true),
                    MonthlyBudget = table.Column<decimal>(type: "numeric(18,9)", precision: 18, scale: 9, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantModelLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantModelLimits_GlobalModels_GlobalModelId",
                        column: x => x.GlobalModelId,
                        principalTable: "GlobalModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderModels_GlobalModelId",
                table: "ProviderModels",
                column: "GlobalModelId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantModelLimits_GlobalModelId",
                table: "TenantModelLimits",
                column: "GlobalModelId");
        }

        /// <inheritdoc />
#pragma warning restore MA0051 // Method is too long

#pragma warning disable MA0051 // Method is too long - Migrations are inherently complex
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderModels");

            migrationBuilder.DropTable(
                name: "TenantModelLimits");

            migrationBuilder.DropTable(
                name: "GlobalModels");
        }
    }
}
