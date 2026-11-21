namespace Chubb.Apac.ClaimsManagement.Application.Security;

public enum ClaimAccessRole
{
    Claimant,
    ClaimsOfficer,
    Manager
}

public sealed record ClaimAccessQuery(
    string ClaimReference,
    ClaimAccessRole Role,
    Guid? ClaimantId = null,
    string? Email = null,
    bool EmailVerified = false,
    Guid? StaffUserId = null);

public sealed record ClaimAccessResult(Guid? ClaimantId);

public sealed record ClaimAccessData(
    string MarketCode,
    IReadOnlyList<ClaimantAccessIdentity> Claimants);

public sealed record ClaimantAccessIdentity(Guid ClaimantId, string? Email);
