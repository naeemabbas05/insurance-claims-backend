using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClaimSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedLossAmount",
                schema: "claims",
                table: "Claims",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "EstimatedLossCurrency",
                schema: "claims",
                table: "Claims",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ClaimSubmissionIdempotency",
                schema: "claims",
                columns: table => new
                {
                    KeyHash = table.Column<string>(type: "char(64)", fixedLength: true, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "char(64)", fixedLength: true, nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimSubmissionIdempotency", x => x.KeyHash);
                    table.ForeignKey(
                        name: "FK_ClaimSubmissionIdempotency_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalSchema: "claims",
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSubmissionIdempotency_ClaimId",
                schema: "claims",
                table: "ClaimSubmissionIdempotency",
                column: "ClaimId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimSubmissionIdempotency",
                schema: "claims");

            migrationBuilder.DropColumn(
                name: "EstimatedLossAmount",
                schema: "claims",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "EstimatedLossCurrency",
                schema: "claims",
                table: "Claims");
        }
    }
}
