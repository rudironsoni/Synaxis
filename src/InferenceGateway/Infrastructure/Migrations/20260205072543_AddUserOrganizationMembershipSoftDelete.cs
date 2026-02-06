// <copyright file="20260205072543_AddUserOrganizationMembershipSoftDelete.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaxis.InferenceGateway.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOrganizationMembershipSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Groups_ParentGroupId",
                schema: "identity",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_ParentGroupId",
                schema: "identity",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ParentGroupId",
                schema: "identity",
                table: "Groups");

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

            migrationBuilder.AlterColumn<Guid>(
                name: "UpdatedBy",
                schema: "identity",
                table: "OrganizationSettings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                schema: "identity",
                table: "OrganizationSettings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaxUsers",
                schema: "identity",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaxRequestBodySizeBytes",
                schema: "identity",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "MaxGroups",
                schema: "identity",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DefaultRateLimitTpm",
                schema: "identity",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DefaultRateLimitRpm",
                schema: "identity",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizationMemberships_DeletedAt",
                schema: "identity",
                table: "UserOrganizationMemberships",
                column: "DeletedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserOrganizationMemberships_DeletedAt",
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

            migrationBuilder.AlterColumn<Guid>(
                name: "UpdatedBy",
                schema: "identity",
                table: "OrganizationSettings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                schema: "identity",
                table: "OrganizationSettings",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "MaxUsers",
                schema: "identity",
                table: "OrganizationSettings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "MaxRequestBodySizeBytes",
                schema: "identity",
                table: "OrganizationSettings",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "MaxGroups",
                schema: "identity",
                table: "OrganizationSettings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "DefaultRateLimitTpm",
                schema: "identity",
                table: "OrganizationSettings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "DefaultRateLimitRpm",
                schema: "identity",
                table: "OrganizationSettings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentGroupId",
                schema: "identity",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ParentGroupId",
                schema: "identity",
                table: "Groups",
                column: "ParentGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Groups_ParentGroupId",
                schema: "identity",
                table: "Groups",
                column: "ParentGroupId",
                principalSchema: "identity",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
