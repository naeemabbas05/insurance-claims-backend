using Chubb.Apac.ClaimsManagement.Application.Claims.Submit;
using Chubb.Apac.ClaimsManagement.Application.Integration;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Application.Tests;

public sealed class ClaimSubmissionTests
{
    private static readonly DateTimeOffset Now = Utc(2026, 3, 12);

    [Fact]
    public async Task ValidatorRejectsInvalidClaimantContactAndFinancialInput()
    {
        var validator = new SubmitClaimCommandValidator(new FixedTimeProvider(Now));
        var command = ValidCommand() with
        {
            ClaimantEmail = null,
            ClaimantPhone = null,
            EstimatedLossAmount = 0
        };

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("contact", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(command.EstimatedLossAmount));
    }

    [Fact]
    public async Task SubmitCreatesClaimWithSubmittedStatusAndReplaysSameIdempotencyKey()
    {
        var repository = new FakeClaimSubmissionRepository(CreatePolicy());
        var eventOutbox = new RecordingClaimIntegrationEventOutbox();
        var service = CreateService(repository, eventOutbox);

        var created = await service.SubmitAsync(ValidCommand(), CancellationToken.None);
        var replay = await service.SubmitAsync(ValidCommand(), CancellationToken.None);

        Assert.Equal(ClaimStatus.Submitted, created.Status);
        Assert.StartsWith("CLM-MY-", created.ClaimReference, StringComparison.Ordinal);
        Assert.Equal(Now, created.SubmittedAtUtc);
        Assert.False(created.IsReplay);
        Assert.True(replay.IsReplay);
        Assert.Equal(created.ClaimReference, replay.ClaimReference);
        Assert.Equal(1, repository.SaveCount);
        Assert.Single(eventOutbox.Events);
        Assert.IsType<ClaimSubmittedV1>(eventOutbox.Events.Single());
    }

    [Fact]
    public async Task SubmitRejectsDifferentPayloadForExistingIdempotencyKey()
    {
        var repository = new FakeClaimSubmissionRepository(CreatePolicy());
        var service = CreateService(repository);
        await service.SubmitAsync(ValidCommand(), CancellationToken.None);

        var changedCommand = ValidCommand() with { EstimatedLossAmount = 2400m };

        await Assert.ThrowsAsync<IdempotencyKeyConflictException>(() =>
            service.SubmitAsync(changedCommand, CancellationToken.None));
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task SubmitAcceptsPropertyClaimsInMatchingMarketAndCurrency()
    {
        var repository = new FakeClaimSubmissionRepository(
            CreatePolicy(PolicyType.Property, "SG", "SGD", "APAC-SG-PROPERTY-001"));
        var service = CreateService(repository);
        var command = ValidCommand() with
        {
            MarketCode = "SG",
            PolicyNumber = "APAC-SG-PROPERTY-001",
            ClaimType = ClaimType.Property,
            CurrencyCode = "SGD"
        };

        var result = await service.SubmitAsync(command, CancellationToken.None);

        Assert.Equal(ClaimStatus.Submitted, result.Status);
        Assert.StartsWith("CLM-SG-", result.ClaimReference, StringComparison.Ordinal);
    }

    private static ClaimSubmissionService CreateService(
        FakeClaimSubmissionRepository repository,
        IClaimIntegrationEventOutbox? eventOutbox = null) =>
        new(
            repository,
            new SubmitClaimCommandValidator(new FixedTimeProvider(Now)),
            eventOutbox ?? new RecordingClaimIntegrationEventOutbox(),
            new FixedTimeProvider(Now));

    private static SubmitClaimCommand ValidCommand() =>
        new(
            "submission-key-123",
            "MY",
            "POL-1234",
            ClaimType.Motor,
            Utc(2026, 3, 10),
            "Vehicle damage following a road accident.",
            "Avery Tan",
            "avery@example.com",
            "+60123456789",
            1200m,
            "MYR");

    private static Policy CreatePolicy(
        PolicyType type = PolicyType.Motor,
        string marketCode = "MY",
        string currencyCode = "MYR",
        string policyNumber = "POL-1234") =>
        Policy.Create(
            PolicyNumber.Create(policyNumber),
            MarketCatalog.Default,
            MarketCode.Create(marketCode),
            type,
            PersonName.Create("Avery Tan"),
            DateRange.Create(Utc(2025, 1, 1), Utc(2027, 1, 1)),
            Money.Create(50000m, currencyCode),
            Utc(2024, 12, 1));

    private static DateTimeOffset Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeClaimSubmissionRepository(Policy policy) : IClaimSubmissionRepository
    {
        private StoredClaimSubmission? _storedSubmission;

        public int SaveCount { get; private set; }

        public Task<bool> IsMarketConfiguredAsync(
            MarketCode marketCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(marketCode == policy.MarketCode);

        public Task<Policy?> FindPolicyAsync(
            MarketCode marketCode,
            PolicyNumber policyNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult<Policy?>(
                marketCode == policy.MarketCode && policyNumber == policy.Number ? policy : null);

        public Task<StoredClaimSubmission?> FindByIdempotencyKeyHashAsync(
            string idempotencyKeyHash,
            CancellationToken cancellationToken) =>
            Task.FromResult(_storedSubmission is null
                ? null
                : _storedSubmission with { IsReplay = true });

        public Task<StoredClaimSubmission> SaveIdempotentlyAsync(
            Claim claim,
            string idempotencyKeyHash,
            string requestFingerprint,
            CancellationToken cancellationToken)
        {
            SaveCount++;
            _storedSubmission = new StoredClaimSubmission(
                requestFingerprint,
                claim.Number.Value,
                claim.Status,
                claim.ReportedAtUtc,
                IsReplay: false);
            return Task.FromResult(_storedSubmission);
        }

        public Task<ClaimTrackingResult?> FindByClaimReferenceAsync(
            ClaimNumber claimNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult<ClaimTrackingResult?>(null);
    }
}
