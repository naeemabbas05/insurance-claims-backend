using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Api.Models;

public sealed class ClaimSubmissionRequest
{
    public string? MarketCode { get; init; }

    public string? PolicyNumber { get; init; }

    public ClaimType? ClaimType { get; init; }

    public DateTimeOffset IncidentAtUtc { get; init; }

    public string? IncidentDescription { get; init; }

    public ClaimantSubmission? Claimant { get; init; }

    public decimal EstimatedLossAmount { get; init; }

    public string? CurrencyCode { get; init; }
}

public sealed class ClaimantSubmission
{
    public string? Name { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }
}
