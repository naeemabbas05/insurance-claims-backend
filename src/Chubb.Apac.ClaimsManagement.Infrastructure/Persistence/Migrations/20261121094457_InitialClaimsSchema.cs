using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialClaimsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "claims");

            migrationBuilder.CreateTable(
                name: "Markets",
                schema: "claims",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DefaultCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Markets", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "StaffUsers",
                schema: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeactivatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Policies",
                schema: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MarketCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    PolicyHolderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CoverageStartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CoverageEndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CoverageLimitAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CoverageLimitCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Policies_Markets_MarketCode",
                        column: x => x.MarketCode,
                        principalSchema: "claims",
                        principalTable: "Markets",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffUserMarkets",
                schema: "claims",
                columns: table => new
                {
                    StaffUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketCode = table.Column<string>(type: "nvarchar(2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffUserMarkets", x => new { x.StaffUserId, x.MarketCode });
                    table.ForeignKey(
                        name: "FK_StaffUserMarkets_Markets_MarketCode",
                        column: x => x.MarketCode,
                        principalSchema: "claims",
                        principalTable: "Markets",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffUserMarkets_StaffUsers_StaffUserId",
                        column: x => x.StaffUserId,
                        principalSchema: "claims",
                        principalTable: "StaffUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Claims",
                schema: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    IncidentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReportedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LossDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Claims_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalSchema: "claims",
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Claimants",
                schema: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claimants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Claimants_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalSchema: "claims",
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimAssessments",
                schema: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessedDamageAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    AssessedDamageCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    RecommendedSettlementAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    RecommendedSettlementCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Findings = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AssessedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimAssessments_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalSchema: "claims",
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimAssessments_StaffUsers_AssessorId",
                        column: x => x.AssessorId,
                        principalSchema: "claims",
                        principalTable: "StaffUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimAssignments",
                schema: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UnassignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimAssignments_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalSchema: "claims",
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimAssignments_StaffUsers_StaffUserId",
                        column: x => x.StaffUserId,
                        principalSchema: "claims",
                        principalTable: "StaffUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimStatusHistory",
                schema: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangedByStaffUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimStatusHistory_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalSchema: "claims",
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimStatusHistory_StaffUsers_ChangedByStaffUserId",
                        column: x => x.ChangedByStaffUserId,
                        principalSchema: "claims",
                        principalTable: "StaffUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimDocuments",
                schema: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UploadedByClaimantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UploadedByStaffUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimDocuments_Claimants_UploadedByClaimantId",
                        column: x => x.UploadedByClaimantId,
                        principalSchema: "claims",
                        principalTable: "Claimants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimDocuments_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalSchema: "claims",
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimDocuments_StaffUsers_UploadedByStaffUserId",
                        column: x => x.UploadedByStaffUserId,
                        principalSchema: "claims",
                        principalTable: "StaffUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimInformationRequests",
                schema: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedFromClaimantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByStaffUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Response = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RespondedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimInformationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimInformationRequests_Claimants_RequestedFromClaimantId",
                        column: x => x.RequestedFromClaimantId,
                        principalSchema: "claims",
                        principalTable: "Claimants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimInformationRequests_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalSchema: "claims",
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimInformationRequests_StaffUsers_RequestedByStaffUserId",
                        column: x => x.RequestedByStaffUserId,
                        principalSchema: "claims",
                        principalTable: "StaffUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimSettlements",
                schema: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayeeClaimantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsFinal = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    ProposedByStaffUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedByStaffUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PaidByStaffUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimSettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimSettlements_Claimants_PayeeClaimantId",
                        column: x => x.PayeeClaimantId,
                        principalSchema: "claims",
                        principalTable: "Claimants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimSettlements_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalSchema: "claims",
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimSettlements_StaffUsers_ApprovedByStaffUserId",
                        column: x => x.ApprovedByStaffUserId,
                        principalSchema: "claims",
                        principalTable: "StaffUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimSettlements_StaffUsers_PaidByStaffUserId",
                        column: x => x.PaidByStaffUserId,
                        principalSchema: "claims",
                        principalTable: "StaffUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimSettlements_StaffUsers_ProposedByStaffUserId",
                        column: x => x.ProposedByStaffUserId,
                        principalSchema: "claims",
                        principalTable: "StaffUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "claims",
                table: "Markets",
                columns: new[] { "Code", "DefaultCurrency", "Name" },
                values: new object[,]
                {
                    { "AU", "AUD", "Australia" },
                    { "ID", "IDR", "Indonesia" },
                    { "KH", "KHR", "Cambodia" },
                    { "MY", "MYR", "Malaysia" },
                    { "SG", "SGD", "Singapore" },
                    { "TH", "THB", "Thailand" }
                });

            migrationBuilder.InsertData(
                schema: "claims",
                table: "StaffUsers",
                columns: new[] { "Id", "CreatedAtUtc", "DeactivatedAtUtc", "Email", "Name", "Role" },
                values: new object[,]
                {
                    { new Guid("f12b52c1-8c11-4aa3-9bc5-c658dc509b01"), new DateTimeOffset(new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "seed.handler.my@chubb.example", "Sample Malaysia Claims Handler", "ClaimsHandler" },
                    { new Guid("f12b52c1-8c11-4aa3-9bc5-c658dc509b02"), new DateTimeOffset(new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "seed.adjuster.id@chubb.example", "Sample Indonesia Adjuster", "Adjuster" },
                    { new Guid("f12b52c1-8c11-4aa3-9bc5-c658dc509b03"), new DateTimeOffset(new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "seed.assessor.sg@chubb.example", "Sample Singapore Assessor", "Assessor" },
                    { new Guid("f12b52c1-8c11-4aa3-9bc5-c658dc509b04"), new DateTimeOffset(new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "seed.approver.kh@chubb.example", "Sample Cambodia Approver", "Approver" },
                    { new Guid("f12b52c1-8c11-4aa3-9bc5-c658dc509b05"), new DateTimeOffset(new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "seed.finance.th@chubb.example", "Sample Thailand Finance Officer", "FinanceOfficer" },
                    { new Guid("f12b52c1-8c11-4aa3-9bc5-c658dc509b06"), new DateTimeOffset(new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "seed.admin.au@chubb.example", "Sample Australia Administrator", "Administrator" }
                });

            migrationBuilder.InsertData(
                schema: "claims",
                table: "Policies",
                columns: new[] { "Id", "CancelledAtUtc", "CreatedAtUtc", "MarketCode", "Number", "PolicyHolderName", "Status", "Type", "CoverageLimitAmount", "CoverageLimitCurrency", "CoverageEndsAtUtc", "CoverageStartsAtUtc" },
                values: new object[,]
                {
                    { new Guid("f12b52c1-8c11-4aa3-9bc5-c658dc509a01"), null, new DateTimeOffset(new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MY", "APAC-MY-MOTOR-001", "Sample Malaysia Motor Policyholder", "Active", "Motor", 200000m, "MYR", new DateTimeOffset(new DateTime(2027, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f12b52c1-8c11-4aa3-9bc5-c658dc509a02"), null, new DateTimeOffset(new DateTime(2025, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SG", "APAC-SG-PROPERTY-001", "Sample Singapore Property Policyholder", "Active", "Property", 500000m, "SGD", new DateTimeOffset(new DateTime(2027, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "claims",
                table: "StaffUserMarkets",
                columns: new[] { "MarketCode", "StaffUserId" },
                values: new object[,]
                {
                    { "MY", new Guid("f12b52c1-8c11-4aa3-9bc5-c658dc509b01") },
                    { "ID", new Guid("f12b52c1-8c11-4aa3-9bc5-c658dc509b02") },
                    { "SG", new Guid("f12b52c1-8c11-4aa3-9bc5-c658dc509b03") },
                    { "KH", new Guid("f12b52c1-8c11-4aa3-9bc5-c658dc509b04") },
                    { "TH", new Guid("f12b52c1-8c11-4aa3-9bc5-c658dc509b05") },
                    { "AU", new Guid("f12b52c1-8c11-4aa3-9bc5-c658dc509b06") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Claimants_ClaimId",
                schema: "claims",
                table: "Claimants",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAssessments_AssessorId",
                schema: "claims",
                table: "ClaimAssessments",
                column: "AssessorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAssessments_ClaimId_AssessedAtUtc",
                schema: "claims",
                table: "ClaimAssessments",
                columns: new[] { "ClaimId", "AssessedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAssignments_ClaimId_StaffUserId_Role",
                schema: "claims",
                table: "ClaimAssignments",
                columns: new[] { "ClaimId", "StaffUserId", "Role" },
                unique: true,
                filter: "[UnassignedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAssignments_StaffUserId",
                schema: "claims",
                table: "ClaimAssignments",
                column: "StaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDocuments_ClaimId",
                schema: "claims",
                table: "ClaimDocuments",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDocuments_UploadedByClaimantId",
                schema: "claims",
                table: "ClaimDocuments",
                column: "UploadedByClaimantId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDocuments_UploadedByStaffUserId",
                schema: "claims",
                table: "ClaimDocuments",
                column: "UploadedByStaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimInformationRequests_ClaimId_Status_DueAtUtc",
                schema: "claims",
                table: "ClaimInformationRequests",
                columns: new[] { "ClaimId", "Status", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimInformationRequests_RequestedByStaffUserId",
                schema: "claims",
                table: "ClaimInformationRequests",
                column: "RequestedByStaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimInformationRequests_RequestedFromClaimantId",
                schema: "claims",
                table: "ClaimInformationRequests",
                column: "RequestedFromClaimantId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Number",
                schema: "claims",
                table: "Claims",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Claims_PolicyId_IncidentAtUtc",
                schema: "claims",
                table: "Claims",
                columns: new[] { "PolicyId", "IncidentAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Status_ReportedAtUtc",
                schema: "claims",
                table: "Claims",
                columns: new[] { "Status", "ReportedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSettlements_ApprovedByStaffUserId",
                schema: "claims",
                table: "ClaimSettlements",
                column: "ApprovedByStaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSettlements_ClaimId_Status",
                schema: "claims",
                table: "ClaimSettlements",
                columns: new[] { "ClaimId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSettlements_PaidByStaffUserId",
                schema: "claims",
                table: "ClaimSettlements",
                column: "PaidByStaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSettlements_PayeeClaimantId",
                schema: "claims",
                table: "ClaimSettlements",
                column: "PayeeClaimantId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimSettlements_ProposedByStaffUserId",
                schema: "claims",
                table: "ClaimSettlements",
                column: "ProposedByStaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimStatusHistory_ChangedByStaffUserId",
                schema: "claims",
                table: "ClaimStatusHistory",
                column: "ChangedByStaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimStatusHistory_ClaimId_ChangedAtUtc",
                schema: "claims",
                table: "ClaimStatusHistory",
                columns: new[] { "ClaimId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Policies_MarketCode_Type_Status",
                schema: "claims",
                table: "Policies",
                columns: new[] { "MarketCode", "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Policies_Number",
                schema: "claims",
                table: "Policies",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffUserMarkets_MarketCode",
                schema: "claims",
                table: "StaffUserMarkets",
                column: "MarketCode");

            migrationBuilder.CreateIndex(
                name: "IX_StaffUsers_Email",
                schema: "claims",
                table: "StaffUsers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimAssessments",
                schema: "claims");

            migrationBuilder.DropTable(
                name: "ClaimAssignments",
                schema: "claims");

            migrationBuilder.DropTable(
                name: "ClaimDocuments",
                schema: "claims");

            migrationBuilder.DropTable(
                name: "ClaimInformationRequests",
                schema: "claims");

            migrationBuilder.DropTable(
                name: "ClaimSettlements",
                schema: "claims");

            migrationBuilder.DropTable(
                name: "ClaimStatusHistory",
                schema: "claims");

            migrationBuilder.DropTable(
                name: "StaffUserMarkets",
                schema: "claims");

            migrationBuilder.DropTable(
                name: "Claimants",
                schema: "claims");

            migrationBuilder.DropTable(
                name: "StaffUsers",
                schema: "claims");

            migrationBuilder.DropTable(
                name: "Claims",
                schema: "claims");

            migrationBuilder.DropTable(
                name: "Policies",
                schema: "claims");

            migrationBuilder.DropTable(
                name: "Markets",
                schema: "claims");
        }
    }
}
