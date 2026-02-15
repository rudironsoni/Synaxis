// <copyright file="20260208232616_AddDataIntegrityConstraints.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaxis.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDataIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_VirtualKey_CurrentSpend_NonNegative",
                table: "virtual_keys",
                sql: "current_spend >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_VirtualKey_MaxBudget_NonNegative",
                table: "virtual_keys",
                sql: "max_budget IS NULL OR max_budget >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Team_BudgetAlertThreshold_Range",
                table: "teams",
                sql: "budget_alert_threshold >= 0 AND budget_alert_threshold <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Team_MonthlyBudget_NonNegative",
                table: "teams",
                sql: "monthly_budget IS NULL OR monthly_budget >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TeamMembership_Role_Valid",
                table: "team_memberships",
                sql: "role IN ('OrgAdmin', 'TeamAdmin', 'Member', 'Viewer')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Organization_Slug_Lowercase",
                table: "organizations",
                sql: "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_VirtualKey_CurrentSpend_NonNegative",
                table: "virtual_keys");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VirtualKey_MaxBudget_NonNegative",
                table: "virtual_keys");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Team_BudgetAlertThreshold_Range",
                table: "teams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Team_MonthlyBudget_NonNegative",
                table: "teams");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TeamMembership_Role_Valid",
                table: "team_memberships");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Organization_Slug_Lowercase",
                table: "organizations");
        }
    }
}
