// <copyright file="20260212191500_AddDeletedAtColumnToUsers.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#pragma warning disable MA0051 // Auto-generated EF Core migrations

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaxis.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SynaxisDbContext))]
    [Migration("20260212191500_AddDeletedAtColumnToUsers")]
    public partial class AddDeletedAtColumnToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "users");
        }
    }
}
