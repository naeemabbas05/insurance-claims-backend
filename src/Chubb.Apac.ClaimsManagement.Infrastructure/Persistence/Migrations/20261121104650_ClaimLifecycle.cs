using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClaimLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedAmount",
                schema: "claims",
                table: "Claims",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedCurrency",
                schema: "claims",
                table: "Claims",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClaimAuditHistory",
                schema: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StaffUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClaimantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimAuditHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimAuditHistory_Claimants_ClaimantId",
                        column: x => x.ClaimantId,
                        principalSchema: "claims",
                        principalTable: "Claimants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimAuditHistory_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalSchema: "claims",
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimAuditHistory_StaffUsers_StaffUserId",
                        column: x => x.StaffUserId,
                        principalSchema: "claims",
                        principalTable: "StaffUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAuditHistory_ClaimantId",
                schema: "claims",
                table: "ClaimAuditHistory",
                column: "ClaimantId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAuditHistory_ClaimId_OccurredAtUtc",
                schema: "claims",
                table: "ClaimAuditHistory",
                columns: new[] { "ClaimId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAuditHistory_StaffUserId",
                schema: "claims",
                table: "ClaimAuditHistory",
                column: "StaffUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimAuditHistory",
                schema: "claims");

            migrationBuilder.DropColumn(
                name: "ApprovedAmount",
                schema: "claims",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "ApprovedCurrency",
                schema: "claims",
                table: "Claims");
        }
    }
}
