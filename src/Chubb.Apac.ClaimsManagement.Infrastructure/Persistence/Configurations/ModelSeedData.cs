using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal static class ModelSeedData
{
    public static readonly Guid MalaysiaMotorPolicyId = Guid.Parse("f12b52c1-8c11-4aa3-9bc5-c658dc509a01");
    public static readonly Guid SingaporePropertyPolicyId = Guid.Parse("f12b52c1-8c11-4aa3-9bc5-c658dc509a02");
    public static readonly Guid MalaysiaHandlerId = Guid.Parse("f12b52c1-8c11-4aa3-9bc5-c658dc509b01");
    public static readonly Guid IndonesiaAdjusterId = Guid.Parse("f12b52c1-8c11-4aa3-9bc5-c658dc509b02");
    public static readonly Guid SingaporeAssessorId = Guid.Parse("f12b52c1-8c11-4aa3-9bc5-c658dc509b03");
    public static readonly Guid CambodiaApproverId = Guid.Parse("f12b52c1-8c11-4aa3-9bc5-c658dc509b04");
    public static readonly Guid ThailandFinanceId = Guid.Parse("f12b52c1-8c11-4aa3-9bc5-c658dc509b05");
    public static readonly Guid AustraliaAdministratorId = Guid.Parse("f12b52c1-8c11-4aa3-9bc5-c658dc509b06");

    public static readonly DateTimeOffset PolicyCreatedAtUtc = Utc(2025, 12, 1);
    public static readonly DateTimeOffset CoverageStartsAtUtc = Utc(2026, 1, 1);
    public static readonly DateTimeOffset CoverageEndsAtUtc = Utc(2027, 1, 1);
    public static readonly DateTimeOffset StaffCreatedAtUtc = Utc(2025, 12, 1);

    public static object[] Policies =>
    [
        new
        {
            Id = MalaysiaMotorPolicyId,
            Number = PolicyNumber.Create("APAC-MY-MOTOR-001"),
            MarketCode = MarketCode.Create("MY"),
            Type = PolicyType.Motor,
            PolicyHolderName = PersonName.Create("Sample Malaysia Motor Policyholder"),
            Status = PolicyStatus.Active,
            CreatedAtUtc = PolicyCreatedAtUtc,
            CancelledAtUtc = (DateTimeOffset?)null
        },
        new
        {
            Id = SingaporePropertyPolicyId,
            Number = PolicyNumber.Create("APAC-SG-PROPERTY-001"),
            MarketCode = MarketCode.Create("SG"),
            Type = PolicyType.Property,
            PolicyHolderName = PersonName.Create("Sample Singapore Property Policyholder"),
            Status = PolicyStatus.Active,
            CreatedAtUtc = PolicyCreatedAtUtc,
            CancelledAtUtc = (DateTimeOffset?)null
        }
    ];

    public static object[] PolicyCoveragePeriods =>
    [
        new
        {
            PolicyId = MalaysiaMotorPolicyId,
            StartsAtUtc = CoverageStartsAtUtc,
            EndsAtUtc = CoverageEndsAtUtc
        },
        new
        {
            PolicyId = SingaporePropertyPolicyId,
            StartsAtUtc = CoverageStartsAtUtc,
            EndsAtUtc = CoverageEndsAtUtc
        }
    ];

    public static object[] PolicyCoverageLimits =>
    [
        new
        {
            PolicyId = MalaysiaMotorPolicyId,
            Amount = 200000m,
            Currency = CurrencyCode.Create("MYR")
        },
        new
        {
            PolicyId = SingaporePropertyPolicyId,
            Amount = 500000m,
            Currency = CurrencyCode.Create("SGD")
        }
    ];

    public static object[] StaffUsers =>
    [
        new
        {
            Id = MalaysiaHandlerId,
            Name = PersonName.Create("Sample Malaysia Claims Handler"),
            Email = EmailAddress.Create("seed.handler.my@chubb.example"),
            Role = StaffRole.ClaimsHandler,
            CreatedAtUtc = StaffCreatedAtUtc,
            DeactivatedAtUtc = (DateTimeOffset?)null
        },
        new
        {
            Id = IndonesiaAdjusterId,
            Name = PersonName.Create("Sample Indonesia Adjuster"),
            Email = EmailAddress.Create("seed.adjuster.id@chubb.example"),
            Role = StaffRole.Adjuster,
            CreatedAtUtc = StaffCreatedAtUtc,
            DeactivatedAtUtc = (DateTimeOffset?)null
        },
        new
        {
            Id = SingaporeAssessorId,
            Name = PersonName.Create("Sample Singapore Assessor"),
            Email = EmailAddress.Create("seed.assessor.sg@chubb.example"),
            Role = StaffRole.Assessor,
            CreatedAtUtc = StaffCreatedAtUtc,
            DeactivatedAtUtc = (DateTimeOffset?)null
        },
        new
        {
            Id = CambodiaApproverId,
            Name = PersonName.Create("Sample Cambodia Approver"),
            Email = EmailAddress.Create("seed.approver.kh@chubb.example"),
            Role = StaffRole.Approver,
            CreatedAtUtc = StaffCreatedAtUtc,
            DeactivatedAtUtc = (DateTimeOffset?)null
        },
        new
        {
            Id = ThailandFinanceId,
            Name = PersonName.Create("Sample Thailand Finance Officer"),
            Email = EmailAddress.Create("seed.finance.th@chubb.example"),
            Role = StaffRole.FinanceOfficer,
            CreatedAtUtc = StaffCreatedAtUtc,
            DeactivatedAtUtc = (DateTimeOffset?)null
        },
        new
        {
            Id = AustraliaAdministratorId,
            Name = PersonName.Create("Sample Australia Administrator"),
            Email = EmailAddress.Create("seed.admin.au@chubb.example"),
            Role = StaffRole.Administrator,
            CreatedAtUtc = StaffCreatedAtUtc,
            DeactivatedAtUtc = (DateTimeOffset?)null
        }
    ];

    public static object[] StaffMarketLinks =>
    [
        new { StaffUserId = MalaysiaHandlerId, MarketCode = MarketCode.Create("MY") },
        new { StaffUserId = IndonesiaAdjusterId, MarketCode = MarketCode.Create("ID") },
        new { StaffUserId = SingaporeAssessorId, MarketCode = MarketCode.Create("SG") },
        new { StaffUserId = CambodiaApproverId, MarketCode = MarketCode.Create("KH") },
        new { StaffUserId = ThailandFinanceId, MarketCode = MarketCode.Create("TH") },
        new { StaffUserId = AustraliaAdministratorId, MarketCode = MarketCode.Create("AU") }
    ];

    private static DateTimeOffset Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);
}
