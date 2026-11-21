using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClaimantNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClaimantNotifications",
                schema: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeduplicationKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimantNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimantNotifications_Claimants_ClaimantId",
                        column: x => x.ClaimantId,
                        principalSchema: "claims",
                        principalTable: "Claimants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimantNotifications_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalSchema: "claims",
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimantNotifications_ClaimantId_CreatedAtUtc",
                schema: "claims",
                table: "ClaimantNotifications",
                columns: new[] { "ClaimantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimantNotifications_ClaimId",
                schema: "claims",
                table: "ClaimantNotifications",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimantNotifications_DeduplicationKey",
                schema: "claims",
                table: "ClaimantNotifications",
                column: "DeduplicationKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimantNotifications",
                schema: "claims");
        }
    }
}
