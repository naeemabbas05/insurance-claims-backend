using Chubb.Apac.ClaimsManagement.Domain.Common;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Domain.Entities;

public sealed class ClaimAssessment : Entity
{
    private ClaimAssessment()
    {
        AssessedDamage = null!;
        RecommendedSettlement = null!;
        Findings = null!;
        Assessor = null!;
    }

    internal ClaimAssessment(
        Guid claimId,
        StaffUser assessor,
        Money assessedDamage,
        Money recommendedSettlement,
        string findings,
        DateTimeOffset assessedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(assessor);
        ArgumentNullException.ThrowIfNull(assessedDamage);
        ArgumentNullException.ThrowIfNull(recommendedSettlement);
        DomainRule.Require(claimId != Guid.Empty, "Claim identifier cannot be empty.");
        DomainRule.Require(assessedDamage.Currency == recommendedSettlement.Currency, "Assessment amounts must use the same currency.");
        DomainRule.Require(
            recommendedSettlement.Amount <= assessedDamage.Amount,
            "Recommended settlement cannot exceed assessed damage.");

        ClaimId = claimId;
        AssessorId = assessor.Id;
        Assessor = assessor;
        AssessedDamage = assessedDamage;
        RecommendedSettlement = recommendedSettlement;
        Findings = DomainRule.RequiredText(findings, nameof(findings), 4000);
        AssessedAtUtc = DomainRule.Utc(assessedAtUtc, nameof(assessedAtUtc));
    }

    public Guid ClaimId { get; private set; }

    public Guid AssessorId { get; private set; }

    public StaffUser Assessor { get; private set; }

    public Money AssessedDamage { get; private set; }

    public Money RecommendedSettlement { get; private set; }

    public string Findings { get; private set; }

    public DateTimeOffset AssessedAtUtc { get; private set; }
}
