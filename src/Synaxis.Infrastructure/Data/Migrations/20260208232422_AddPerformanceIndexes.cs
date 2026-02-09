using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaxis.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_team_memberships_team_id",
                table: "team_memberships");

            migrationBuilder.CreateIndex(
                name: "IX_virtual_keys_organization_id_name",
                table: "virtual_keys",
                columns: new[] { "organization_id", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_teams_organization_id_name",
                table: "teams",
                columns: new[] { "organization_id", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_team_memberships_team_id_user_id",
                table: "team_memberships",
                columns: new[] { "team_id", "user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_virtual_keys_organization_id_name",
                table: "virtual_keys");

            migrationBuilder.DropIndex(
                name: "IX_teams_organization_id_name",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_team_memberships_team_id_user_id",
                table: "team_memberships");

            migrationBuilder.CreateIndex(
                name: "IX_team_memberships_team_id",
                table: "team_memberships",
                column: "team_id");
        }
    }
}
