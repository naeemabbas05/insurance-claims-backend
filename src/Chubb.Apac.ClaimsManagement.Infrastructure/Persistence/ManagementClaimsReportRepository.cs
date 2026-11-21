using System.Linq.Expressions;
using Chubb.Apac.ClaimsManagement.Application.ManagementReporting;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;

public sealed class ManagementClaimsReportRepository(ClaimsDbContext dbContext)
    : IManagementClaimsReportRepository
{
    public Task<StaffUser?> FindStaffUserAsync(
        Guid staffUserId,
        CancellationToken cancellationToken) =>
        dbContext.StaffUsers
            .AsNoTracking()
            .Include(staffUser => staffUser.Markets)
            .SingleOrDefaultAsync(staffUser => staffUser.Id == staffUserId, cancellationToken);

    public async Task<ManagementClaimsReportData> GetReportAsync(
        IReadOnlyCollection<MarketCode> marketCodes,
        DateTimeOffset? fromDateUtc,
        DateTimeOffset? toDateExclusiveUtc,
        CancellationToken cancellationToken)
    {
        var claims = BuildFilteredClaimsQuery(marketCodes, fromDateUtc, toDateExclusiveUtc);

        var totalClaims = await claims.CountAsync(cancellationToken);
        var unassignedClaims = await claims
            .Where(claim =>
                claim.Status != ClaimStatus.Closed &&
                claim.Status != ClaimStatus.Settled &&
                !claim.Assignments.Any(assignment => assignment.UnassignedAtUtc == null))
            .CountAsync(cancellationToken);

        var claimsByStatusRows = await claims
            .GroupBy(claim => claim.Status)
            .OrderBy(group => group.Key)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToArrayAsync(cancellationToken);
        var claimsByStatus = claimsByStatusRows
            .Select(item => new ClaimCountByStatus(item.Status, item.Count))
            .ToArray();
        var claimsByMarketRows = await claims
            .GroupBy(claim => claim.Policy.MarketCode)
            .OrderBy(group => group.Key)
            .Select(group => new { MarketCode = group.Key, Count = group.Count() })
            .ToArrayAsync(cancellationToken);
        var claimsByMarket = claimsByMarketRows
            .Select(item => new ClaimCountByMarket(item.MarketCode.Value, item.Count))
            .ToArray();
        var claimsByTypeRows = await claims
            .GroupBy(claim => claim.Type)
            .OrderBy(group => group.Key)
            .Select(group => new { Type = group.Key, Count = group.Count() })
            .ToArrayAsync(cancellationToken);
        var claimsByType = claimsByTypeRows
            .Select(item => new ClaimCountByType(item.Type, item.Count))
            .ToArray();

        var activeAssignments = claims
            .Where(claim =>
                claim.Status != ClaimStatus.Closed &&
                claim.Status != ClaimStatus.Settled)
            .SelectMany(claim => claim.Assignments
                .Where(assignment => assignment.UnassignedAtUtc == null)
                .Select(assignment => new { assignment.StaffUserId, assignment.ClaimId }));
        var workloadCounts = await activeAssignments
            .GroupBy(assignment => assignment.StaffUserId)
            .Select(group => new
            {
                OfficerId = group.Key,
                ActiveClaimCount = group.Select(assignment => assignment.ClaimId).Distinct().Count()
            })
            .OrderByDescending(item => item.ActiveClaimCount)
            .ThenBy(item => item.OfficerId)
            .ToArrayAsync(cancellationToken);
        var officerIds = workloadCounts.Select(item => item.OfficerId).ToArray();
        var officerNames = officerIds.Length == 0
            ? new Dictionary<Guid, string>()
            : (await dbContext.StaffUsers
                .AsNoTracking()
                .Where(staffUser => officerIds.Contains(staffUser.Id))
                .Select(staffUser => new { staffUser.Id, staffUser.Name })
                .ToArrayAsync(cancellationToken))
                .ToDictionary(item => item.Id, item => item.Name.FullName);
        var workloadByOfficer = workloadCounts
            .Select(item => new OfficerClaimWorkload(
                item.OfficerId,
                officerNames[item.OfficerId],
                item.ActiveClaimCount))
            .ToArray();

        var estimatedExposureRows = await claims
            .Where(claim =>
                claim.Status != ClaimStatus.Closed &&
                claim.Status != ClaimStatus.Settled &&
                claim.Status != ClaimStatus.Declined)
            .GroupBy(claim => claim.EstimatedLoss.Currency)
            .OrderBy(group => group.Key)
            .Select(group => new
            {
                Currency = group.Key,
                Amount = group.Sum(claim => claim.EstimatedLoss.Amount)
            })
            .ToArrayAsync(cancellationToken);
        var outstandingEstimatedExposure = estimatedExposureRows
            .Select(item => new CurrencyAmountTotal(item.Currency.Value, item.Amount))
            .ToArray();

        var latestAssessments = claims.SelectMany(claim => claim.Assessments
            .OrderByDescending(assessment => assessment.AssessedAtUtc)
            .ThenByDescending(assessment => assessment.Id)
            .Take(1));
        var assessedAmountRows = await latestAssessments
            .GroupBy(assessment => assessment.AssessedDamage.Currency)
            .OrderBy(group => group.Key)
            .Select(group => new
            {
                Currency = group.Key,
                Amount = group.Sum(assessment => assessment.AssessedDamage.Amount)
            })
            .ToArrayAsync(cancellationToken);
        var assessedAmount = assessedAmountRows
            .Select(item => new CurrencyAmountTotal(item.Currency.Value, item.Amount))
            .ToArray();

        var approvedAmountRows = await claims
            .Where(claim => claim.ApprovedAmount != null)
            .GroupBy(claim => claim.ApprovedAmount!.Currency)
            .OrderBy(group => group.Key)
            .Select(group => new
            {
                Currency = group.Key,
                Amount = group.Sum(claim => claim.ApprovedAmount!.Amount)
            })
            .ToArrayAsync(cancellationToken);
        var approvedAmount = approvedAmountRows
            .Select(item => new CurrencyAmountTotal(item.Currency.Value, item.Amount))
            .ToArray();

        var settlements = claims.SelectMany(claim => claim.Settlements);
        var approvedButUnpaidLiabilityRows = await settlements
            .Where(settlement => settlement.Status == SettlementStatus.Approved)
            .GroupBy(settlement => settlement.Amount.Currency)
            .OrderBy(group => group.Key)
            .Select(group => new
            {
                Currency = group.Key,
                Amount = group.Sum(settlement => settlement.Amount.Amount)
            })
            .ToArrayAsync(cancellationToken);
        var approvedButUnpaidLiability = approvedButUnpaidLiabilityRows
            .Select(item => new CurrencyAmountTotal(item.Currency.Value, item.Amount))
            .ToArray();
        var settledAmountRows = await settlements
            .Where(settlement => settlement.Status == SettlementStatus.Paid)
            .GroupBy(settlement => settlement.Amount.Currency)
            .OrderBy(group => group.Key)
            .Select(group => new
            {
                Currency = group.Key,
                Amount = group.Sum(settlement => settlement.Amount.Amount)
            })
            .ToArrayAsync(cancellationToken);
        var settledAmount = settledAmountRows
            .Select(item => new CurrencyAmountTotal(item.Currency.Value, item.Amount))
            .ToArray();

        return new ManagementClaimsReportData(
            totalClaims,
            unassignedClaims,
            claimsByStatus,
            claimsByMarket,
            claimsByType,
            workloadByOfficer,
            outstandingEstimatedExposure,
            assessedAmount,
            approvedAmount,
            approvedButUnpaidLiability,
            settledAmount);
    }

    public async Task<ClaimsAgingReportData> GetAgingReportAsync(
        IReadOnlyCollection<MarketCode> marketCodes,
        DateTimeOffset? fromDateUtc,
        DateTimeOffset? toDateExclusiveUtc,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
    {
        var openClaims = BuildFilteredClaimsQuery(marketCodes, fromDateUtc, toDateExclusiveUtc)
            .Where(claim =>
                claim.Status != ClaimStatus.Closed &&
                claim.Status != ClaimStatus.Settled &&
                claim.Status != ClaimStatus.Declined);
        var rows = await openClaims
            .Select(claim => EF.Functions.DateDiffDay(claim.ReportedAtUtc, asOfUtc))
            .GroupBy(ageDays =>
                ageDays <= 7 ? 0 :
                ageDays <= 14 ? 1 :
                ageDays <= 30 ? 2 :
                ageDays <= 60 ? 3 : 4)
            .Select(group => new
            {
                Bucket = group.Key,
                Count = group.Count()
            })
            .ToArrayAsync(cancellationToken);

        var counts = rows.ToDictionary(item => item.Bucket, item => item.Count);
        var buckets = new[]
        {
            new ClaimAgingBucket("0-7 days", counts.GetValueOrDefault(0)),
            new ClaimAgingBucket("8-14 days", counts.GetValueOrDefault(1)),
            new ClaimAgingBucket("15-30 days", counts.GetValueOrDefault(2)),
            new ClaimAgingBucket("31-60 days", counts.GetValueOrDefault(3)),
            new ClaimAgingBucket("Over 60 days", counts.GetValueOrDefault(4))
        };

        return new ClaimsAgingReportData(
            buckets.Sum(bucket => bucket.Count),
            buckets);
    }

    public async Task<ClaimsPerformanceReportData> GetPerformanceReportAsync(
        IReadOnlyCollection<MarketCode> marketCodes,
        DateTimeOffset? fromDateUtc,
        DateTimeOffset? toDateExclusiveUtc,
        DateTimeOffset asOfUtc,
        int targetDays,
        CancellationToken cancellationToken)
    {
        var claims = await BuildFilteredClaimsQuery(marketCodes, fromDateUtc, toDateExclusiveUtc)
            .Select(claim => new
            {
                claim.Status,
                claim.ReportedAtUtc,
                DecisionAtUtc = claim.StatusHistory
                    .Where(history =>
                        history.NewStatus == ClaimStatus.Approved ||
                        history.NewStatus == ClaimStatus.Declined)
                    .OrderByDescending(history => history.ChangedAtUtc)
                    .ThenByDescending(history => history.Id)
                    .Select(history => (DateTimeOffset?)history.ChangedAtUtc)
                    .FirstOrDefault(),
                LatestReviewAtUtc = claim.StatusHistory
                    .Where(history => history.NewStatus == ClaimStatus.UnderReview)
                    .Select(history => (DateTimeOffset?)history.ChangedAtUtc)
                    .Max()
            })
            .ToArrayAsync(cancellationToken);
        var completedClaims = claims
            .Where(claim =>
                IsFinalStatus(claim.Status) &&
                claim.DecisionAtUtc.HasValue &&
                claim.DecisionAtUtc.Value <= asOfUtc &&
                (!claim.LatestReviewAtUtc.HasValue ||
                 claim.DecisionAtUtc.Value >= claim.LatestReviewAtUtc.Value))
            .Select(claim => new
            {
                TurnaroundDays = GetTurnaroundDays(claim.ReportedAtUtc, claim.DecisionAtUtc!.Value)
            })
            .ToArray();
        var openStatuses = claims
            .Where(claim => !IsFinalStatus(claim.Status))
            .ToArray();
        var olderThan30Days = openStatuses.Count(claim =>
            claim.ReportedAtUtc < asOfUtc.AddDays(-30));
        var averageTurnaroundDays = completedClaims.Length == 0
            ? 0m
            : decimal.Round(
                completedClaims.Sum(claim => claim.TurnaroundDays) / completedClaims.Length,
                1);
        var achieved = completedClaims.Count(claim => claim.TurnaroundDays <= targetDays);

        return new ClaimsPerformanceReportData(
            claims.Length,
            completedClaims.Length,
            claims.Length - completedClaims.Length,
            averageTurnaroundDays,
            olderThan30Days,
            completedClaims.Length,
            achieved,
            completedClaims.Length - achieved);
    }

    private static bool IsFinalStatus(ClaimStatus status) =>
        status is ClaimStatus.Approved or
            ClaimStatus.Declined or
            ClaimStatus.Settled or
            ClaimStatus.Closed;

    private static decimal GetTurnaroundDays(
        DateTimeOffset reportedAtUtc,
        DateTimeOffset decisionAtUtc) =>
        (decimal)(decisionAtUtc - reportedAtUtc).Ticks / TimeSpan.TicksPerDay;

    private IQueryable<Claim> BuildFilteredClaimsQuery(
        IReadOnlyCollection<MarketCode> marketCodes,
        DateTimeOffset? fromDateUtc,
        DateTimeOffset? toDateExclusiveUtc)
    {
        var claims = dbContext.Claims
            .AsNoTracking()
            .Where(BuildMarketPredicate(marketCodes));
        if (fromDateUtc.HasValue)
        {
            claims = claims.Where(claim => claim.ReportedAtUtc >= fromDateUtc.Value);
        }

        if (toDateExclusiveUtc.HasValue)
        {
            claims = claims.Where(claim => claim.ReportedAtUtc < toDateExclusiveUtc.Value);
        }

        return claims;
    }

    private static Expression<Func<Claim, bool>> BuildMarketPredicate(
        IReadOnlyCollection<MarketCode> marketCodes)
    {
        var claimParameter = Expression.Parameter(typeof(Claim), "claim");
        var marketCodeProperty = Expression.Property(
            Expression.Property(claimParameter, nameof(Claim.Policy)),
            nameof(Policy.MarketCode));
        Expression body = Expression.Constant(false);
        foreach (var marketCode in marketCodes)
        {
            body = Expression.OrElse(
                body,
                Expression.Equal(marketCodeProperty, Expression.Constant(marketCode)));
        }

        return Expression.Lambda<Func<Claim, bool>>(body, claimParameter);
    }
}
