// <copyright file="20260212191500_AddDeletedAtColumnToUsers.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Synaxis.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedAtColumnToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddDeletedAtColumn(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropDeletedAtColumn(migrationBuilder);
        }

        private static void AddDeletedAtColumn(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        private static void DropDeletedAtColumn(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "users");
        }
    }
}
