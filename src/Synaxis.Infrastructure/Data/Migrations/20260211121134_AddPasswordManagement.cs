// <copyright file="20260211121134_AddPasswordManagement.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#pragma warning disable MA0051 // Auto-generated EF Core migrations

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaxis.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failed_password_change_attempts",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "must_change_password",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_change_locked_until",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_changed_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_expires_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "password_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    set_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_histories", x => x.id);
                    table.ForeignKey(
                        name: "FK_password_histories_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    min_length = table.Column<int>(type: "integer", nullable: false),
                    require_uppercase = table.Column<bool>(type: "boolean", nullable: false),
                    require_lowercase = table.Column<bool>(type: "boolean", nullable: false),
                    require_numbers = table.Column<bool>(type: "boolean", nullable: false),
                    require_special_characters = table.Column<bool>(type: "boolean", nullable: false),
                    password_history_count = table.Column<int>(type: "integer", nullable: false),
                    password_expiration_days = table.Column<int>(type: "integer", nullable: false),
                    password_expiration_warning_days = table.Column<int>(type: "integer", nullable: false),
                    max_failed_change_attempts = table.Column<int>(type: "integer", nullable: false),
                    lockout_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    block_common_passwords = table.Column<bool>(type: "boolean", nullable: false),
                    block_user_info_in_password = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_policies", x => x.id);
                    table.ForeignKey(
                        name: "FK_password_policies_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_password_histories_user_id",
                table: "password_histories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_password_histories_user_id_set_at",
                table: "password_histories",
                columns: new[] { "user_id", "set_at" });

            migrationBuilder.CreateIndex(
                name: "IX_password_policies_organization_id",
                table: "password_policies",
                column: "organization_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_histories");

            migrationBuilder.DropTable(
                name: "password_policies");

            migrationBuilder.DropColumn(
                name: "failed_password_change_attempts",
                table: "users");

            migrationBuilder.DropColumn(
                name: "must_change_password",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_change_locked_until",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_changed_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_expires_at",
                table: "users");
        }
    }
}
