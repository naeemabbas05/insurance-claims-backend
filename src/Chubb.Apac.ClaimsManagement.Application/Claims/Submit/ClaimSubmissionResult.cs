using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Submit;

public sealed record ClaimSubmissionResult(
    string ClaimReference,
    ClaimStatus Status,
    DateTimeOffset SubmittedAtUtc,
    bool IsReplay);

public sealed record ClaimTrackingResult(
    string ClaimReference,
    ClaimStatus Status,
    DateTimeOffset SubmittedAtUtc);

public sealed record StoredClaimSubmission(
    string RequestFingerprint,
    string ClaimReference,
    ClaimStatus Status,
    DateTimeOffset SubmittedAtUtc,
    bool IsReplay);
