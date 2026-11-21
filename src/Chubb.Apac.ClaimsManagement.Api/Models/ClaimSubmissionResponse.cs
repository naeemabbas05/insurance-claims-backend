using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Api.Models;

public sealed record ClaimSubmissionResponse(
    string ClaimReference,
    ClaimStatus Status,
    DateTimeOffset SubmittedAtUtc,
    string TrackingUrl);

public sealed record ClaimTrackingResponse(
    string ClaimReference,
    ClaimStatus Status,
    DateTimeOffset SubmittedAtUtc);
