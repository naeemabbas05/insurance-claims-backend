using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Chubb.Apac.ClaimsManagement.Domain.Common;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Submit;

public sealed class ClaimSubmissionService(
    IClaimSubmissionRepository repository,
    IValidator<SubmitClaimCommand> validator,
    TimeProvider timeProvider) : IClaimSubmissionService
{
    public async Task<ClaimSubmissionResult> SubmitAsync(
        SubmitClaimCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var marketCode = MarketCode.Create(command.MarketCode);
        var policyNumber = PolicyNumber.Create(command.PolicyNumber);
        var currencyCode = CurrencyCode.Create(command.CurrencyCode);
        var claimantName = PersonName.Create(command.ClaimantName);
        var contact = ContactDetails.Create(command.ClaimantEmail, command.ClaimantPhone);
        var lossDescription = DomainRule.RequiredText(
            command.IncidentDescription,
            nameof(command.IncidentDescription),
            4000);
        var requestFingerprint = CreateRequestFingerprint(
            command,
            marketCode,
            policyNumber,
            currencyCode,
            claimantName,
            contact,
            lossDescription);
        var idempotencyKeyHash = Hash(command.IdempotencyKey);

        var existingSubmission = await repository.FindByIdempotencyKeyHashAsync(
            idempotencyKeyHash,
            cancellationToken);
        if (existingSubmission is not null)
        {
            return ToResult(existingSubmission, requestFingerprint);
        }

        if (!await repository.IsMarketConfiguredAsync(marketCode, cancellationToken))
        {
            throw new MarketNotConfiguredException(marketCode.Value);
        }

        var policy = await repository.FindPolicyAsync(marketCode, policyNumber, cancellationToken)
            ?? throw new PolicyNotFoundException(policyNumber.Value, marketCode.Value);

        var reportedAtUtc = timeProvider.GetUtcNow();
        var claimReference = ClaimNumber.Create($"CLM-{marketCode.Value}-{Guid.NewGuid():N}");
        var claim = Claim.Create(
            claimReference,
            policy,
            command.ClaimType!.Value,
            command.IncidentAtUtc,
            reportedAtUtc,
            lossDescription,
            Money.Create(command.EstimatedLossAmount, currencyCode),
            ClaimantDetails.Create(ClaimantRole.PolicyHolder, claimantName, contact));

        var savedSubmission = await repository.SaveIdempotentlyAsync(
            claim,
            idempotencyKeyHash,
            requestFingerprint,
            cancellationToken);
        return ToResult(savedSubmission, requestFingerprint);
    }

    public async Task<ClaimTrackingResult?> GetTrackingAsync(
        string claimReference,
        CancellationToken cancellationToken)
    {
        if (!IsValidClaimReference(claimReference))
        {
            return null;
        }

        return await repository.FindByClaimReferenceAsync(
            ClaimNumber.Create(claimReference),
            cancellationToken);
    }

    private static ClaimSubmissionResult ToResult(
        StoredClaimSubmission submission,
        string expectedFingerprint)
    {
        if (!string.Equals(
                submission.RequestFingerprint,
                expectedFingerprint,
                StringComparison.Ordinal))
        {
            throw new IdempotencyKeyConflictException();
        }

        return new ClaimSubmissionResult(
            submission.ClaimReference,
            submission.Status,
            submission.SubmittedAtUtc,
            submission.IsReplay);
    }

    private static string CreateRequestFingerprint(
        SubmitClaimCommand command,
        MarketCode marketCode,
        PolicyNumber policyNumber,
        CurrencyCode currencyCode,
        PersonName claimantName,
        ContactDetails contact,
        string lossDescription)
    {
        var canonicalRequest = JsonSerializer.Serialize(new
        {
            MarketCode = marketCode.Value,
            PolicyNumber = policyNumber.Value,
            ClaimType = command.ClaimType!.Value.ToString(),
            IncidentAtUtc = command.IncidentAtUtc.ToString("O", CultureInfo.InvariantCulture),
            IncidentDescription = lossDescription,
            ClaimantName = claimantName.FullName,
            ClaimantEmail = contact.Email?.Value,
            ClaimantPhone = contact.Phone?.Value,
            EstimatedLossAmount = command.EstimatedLossAmount.ToString("G29", CultureInfo.InvariantCulture),
            CurrencyCode = currencyCode.Value
        });

        return Hash(canonicalRequest);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool IsValidClaimReference(string? value) =>
        value is { Length: 39 } &&
        value.StartsWith("CLM-", StringComparison.Ordinal) &&
        value[4..6].All(character => character is >= 'A' and <= 'Z') &&
        value[6] == '-' &&
        value[7..].All(Uri.IsHexDigit);
}
