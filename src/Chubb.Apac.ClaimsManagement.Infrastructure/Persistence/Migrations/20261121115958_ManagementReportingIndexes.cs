using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ManagementReportingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Claims_ReportedAtUtc_PolicyId_Status_Type",
                schema: "claims",
                table: "Claims",
                columns: new[] { "ReportedAtUtc", "PolicyId", "Status", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAssignments_StaffUserId_ClaimId",
                schema: "claims",
                table: "ClaimAssignments",
                columns: new[] { "StaffUserId", "ClaimId" },
                filter: "[UnassignedAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Claims_ReportedAtUtc_PolicyId_Status_Type",
                schema: "claims",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_ClaimAssignments_StaffUserId_ClaimId",
                schema: "claims",
                table: "ClaimAssignments");
        }
    }
}
