// <copyright file="20260210223935_MfaBackupCodesAndJwtBlacklist.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#pragma warning disable MA0051 // Auto-generated EF Core migrations

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaxis.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MfaBackupCodesAndJwtBlacklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MfaBackupCodes",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Collections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.id);
                    table.CheckConstraint("CK_Collection_Type_Valid", "type IN ('general', 'models', 'prompts', 'datasets', 'workflows')");
                    table.CheckConstraint("CK_Collection_Visibility_Valid", "visibility IN ('public', 'private', 'team')");
                    table.ForeignKey(
                        name: "FK_Collections_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Collections_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Collections_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JwtBlacklists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JwtBlacklists", x => x.id);
                    table.ForeignKey(
                        name: "FK_JwtBlacklists_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectionMemberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    added_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionMemberships", x => x.id);
                    table.CheckConstraint("CK_CollectionMembership_Role_Valid", "role IN ('Admin', 'Member', 'Viewer')");
                    table.ForeignKey(
                        name: "FK_CollectionMemberships_Collections_collection_id",
                        column: x => x.collection_id,
                        principalTable: "Collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionMemberships_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionMemberships_users_added_by",
                        column: x => x.added_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionMemberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionMemberships_added_by",
                table: "CollectionMemberships",
                column: "added_by");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionMemberships_collection_id_user_id",
                table: "CollectionMemberships",
                columns: new[] { "collection_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionMemberships_organization_id_user_id",
                table: "CollectionMemberships",
                columns: new[] { "organization_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionMemberships_user_id_collection_id",
                table: "CollectionMemberships",
                columns: new[] { "user_id", "collection_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collections_created_by",
                table: "Collections",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_is_active",
                table: "Collections",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_organization_id_name",
                table: "Collections",
                columns: new[] { "organization_id", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_Collections_organization_id_slug",
                table: "Collections",
                columns: new[] { "organization_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collections_team_id",
                table: "Collections",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_type",
                table: "Collections",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_visibility",
                table: "Collections",
                column: "visibility");

            migrationBuilder.CreateIndex(
                name: "IX_JwtBlacklists_expires_at",
                table: "JwtBlacklists",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_JwtBlacklists_token_id",
                table: "JwtBlacklists",
                column: "token_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JwtBlacklists_user_id",
                table: "JwtBlacklists",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionMemberships");

            migrationBuilder.DropTable(
                name: "JwtBlacklists");

            migrationBuilder.DropTable(
                name: "Collections");

            migrationBuilder.DropColumn(
                name: "MfaBackupCodes",
                table: "users");
        }
    }
}
