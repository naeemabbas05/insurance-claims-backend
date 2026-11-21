using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Application.ManagementReporting;

public sealed class ManagementClaimsReportingService(
    IManagementClaimsReportRepository repository,
    IValidator<ManagementClaimsReportQuery> queryValidator) : IManagementClaimsReportingService
{
    public async Task<ManagementClaimsReportResult> GetClaimsReportAsync(
        ManagementClaimsReportQuery query,
        CancellationToken cancellationToken)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var actor = await repository.FindStaffUserAsync(query.ActorStaffUserId, cancellationToken);
        if (actor is not { IsActive: true } ||
            (!actor.CanPerform(StaffRole.Approver) && !actor.CanPerform(StaffRole.FinanceOfficer)))
        {
            throw new ManagementReportAccessDeniedException();
        }

        IReadOnlyCollection<MarketCode> markets;
        if (query.MarketCode is not null)
        {
            var marketCode = MarketCode.Create(query.MarketCode);
            if (!actor.CanWorkIn(marketCode))
            {
                throw new ManagementReportAccessDeniedException();
            }

            markets = [marketCode];
        }
        else
        {
            markets = actor.Markets
                .Select(market => market.Code)
                .Distinct()
                .ToArray();
        }

        DateTimeOffset? fromDateUtc = query.FromDate is { } fromDate
            ? new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            : null;
        DateTimeOffset? toDateExclusiveUtc = query.ToDate is { } toDate && toDate < DateOnly.MaxValue
            ? new DateTimeOffset(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            : null;

        var report = await repository.GetReportAsync(
            markets,
            fromDateUtc,
            toDateExclusiveUtc,
            cancellationToken);

        return new ManagementClaimsReportResult(
            query.MarketCode?.ToUpperInvariant(),
            query.FromDate,
            query.ToDate,
            report.TotalClaims,
            report.UnassignedClaims,
            report.ClaimsByStatus,
            report.ClaimsByMarket,
            report.ClaimsByType,
            report.WorkloadByOfficer,
            report.OutstandingEstimatedExposure,
            report.AssessedAmount,
            report.ApprovedAmount,
            report.ApprovedButUnpaidLiability,
            report.SettledAmount);
    }
}
