using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Submit;

public sealed record SubmitClaimCommand(
    string IdempotencyKey,
    string MarketCode,
    string PolicyNumber,
    ClaimType? ClaimType,
    DateTimeOffset IncidentAtUtc,
    string IncidentDescription,
    string ClaimantName,
    string? ClaimantEmail,
    string? ClaimantPhone,
    decimal EstimatedLossAmount,
    string CurrencyCode);
