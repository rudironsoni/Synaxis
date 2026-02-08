// <copyright file="20260208013205_PendingModelChanges.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "identity",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "identity",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "identity",
                table: "UserOrganizationMemberships",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "identity",
                table: "UserOrganizationMemberships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                schema: "identity",
                table: "UserOrganizationMemberships",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedAt",
                schema: "identity",
                table: "UserOrganizationMemberships",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "identity",
                table: "UserOrganizationMemberships",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "identity",
                table: "UserOrganizationMemberships");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "identity",
                table: "UserOrganizationMemberships");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "identity",
                table: "UserOrganizationMemberships");

            migrationBuilder.DropColumn(
                name: "JoinedAt",
                schema: "identity",
                table: "UserOrganizationMemberships");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "identity",
                table: "UserOrganizationMemberships");
        }
    }
}
