// <copyright file="20260205072543_AddUserOrganizationMembershipSoftDelete.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;
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
            DropGroupParentReference(migrationBuilder);
            AddSoftDeleteColumns(migrationBuilder);
            UpdateOrganizationSettingDefaults(migrationBuilder);
            CreateSoftDeleteIndex(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropSoftDeleteIndex(migrationBuilder);
            RemoveSoftDeleteColumns(migrationBuilder);
            RestoreOrganizationSettingDefaults(migrationBuilder);
            RestoreGroupParentReference(migrationBuilder);
        }

        private static void DropGroupParentReference(MigrationBuilder migrationBuilder)
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
        }

        private static void AddSoftDeleteColumns(MigrationBuilder migrationBuilder)
        {
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
        }

        private static void UpdateOrganizationSettingDefaults(MigrationBuilder migrationBuilder)
        {
            SetUpdatedByDefault(migrationBuilder);
            SetUpdatedAtDefault(migrationBuilder);
            SetMaxUsersDefault(migrationBuilder);
            SetMaxRequestBodySizeDefault(migrationBuilder);
            SetMaxGroupsDefault(migrationBuilder);
            SetDefaultRateLimitTpmDefault(migrationBuilder);
            SetDefaultRateLimitRpmDefault(migrationBuilder);
        }

        private static void CreateSoftDeleteIndex(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizationMemberships_DeletedAt",
                schema: "identity",
                table: "UserOrganizationMemberships",
                column: "DeletedAt");
        }

        private static void DropSoftDeleteIndex(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserOrganizationMemberships_DeletedAt",
                schema: "identity",
                table: "UserOrganizationMemberships");
        }

        private static void RemoveSoftDeleteColumns(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "identity",
                table: "UserOrganizationMemberships");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "identity",
                table: "UserOrganizationMemberships");
        }

        private static void RestoreOrganizationSettingDefaults(MigrationBuilder migrationBuilder)
        {
            RestoreUpdatedByDefault(migrationBuilder);
            RestoreUpdatedAtDefault(migrationBuilder);
            RestoreMaxUsersDefault(migrationBuilder);
            RestoreMaxRequestBodySizeDefault(migrationBuilder);
            RestoreMaxGroupsDefault(migrationBuilder);
            RestoreDefaultRateLimitTpmDefault(migrationBuilder);
            RestoreDefaultRateLimitRpmDefault(migrationBuilder);
        }

        private static void SetUpdatedByDefault(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UpdatedBy",
                schema: "identity",
                table: "OrganizationSettings",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        private static void SetUpdatedAtDefault(MigrationBuilder migrationBuilder)
        {
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
        }

        private static void SetMaxUsersDefault(MigrationBuilder migrationBuilder)
        {
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
        }

        private static void SetMaxRequestBodySizeDefault(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MaxRequestBodySizeBytes",
                schema: "identity",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }

        private static void SetMaxGroupsDefault(MigrationBuilder migrationBuilder)
        {
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
        }

        private static void SetDefaultRateLimitTpmDefault(MigrationBuilder migrationBuilder)
        {
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
        }

        private static void SetDefaultRateLimitRpmDefault(MigrationBuilder migrationBuilder)
        {
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
        }

        private static void RestoreUpdatedByDefault(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UpdatedBy",
                schema: "identity",
                table: "OrganizationSettings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        private static void RestoreUpdatedAtDefault(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                schema: "identity",
                table: "OrganizationSettings",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        private static void RestoreMaxUsersDefault(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MaxUsers",
                schema: "identity",
                table: "OrganizationSettings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        private static void RestoreMaxRequestBodySizeDefault(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "MaxRequestBodySizeBytes",
                schema: "identity",
                table: "OrganizationSettings",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        private static void RestoreMaxGroupsDefault(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MaxGroups",
                schema: "identity",
                table: "OrganizationSettings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        private static void RestoreDefaultRateLimitTpmDefault(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DefaultRateLimitTpm",
                schema: "identity",
                table: "OrganizationSettings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        private static void RestoreDefaultRateLimitRpmDefault(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DefaultRateLimitRpm",
                schema: "identity",
                table: "OrganizationSettings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        private static void RestoreGroupParentReference(MigrationBuilder migrationBuilder)
        {
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
