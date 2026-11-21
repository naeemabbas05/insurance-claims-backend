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
            .Select(claim => new
            {
                claim.Policy.MarketCode,
                AgeDays = EF.Functions.DateDiffDay(claim.ReportedAtUtc, asOfUtc)
            })
            .GroupBy(item => new
            {
                item.MarketCode,
                Bucket = item.AgeDays <= 7 ? 0 : item.AgeDays <= 14 ? 1 : item.AgeDays <= 30 ? 2 : 3
            })
            .Select(group => new
            {
                group.Key.MarketCode,
                group.Key.Bucket,
                Count = group.Count()
            })
            .ToArrayAsync(cancellationToken);

        var markets = rows
            .GroupBy(item => item.MarketCode.Value)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var counts = group.ToDictionary(item => item.Bucket, item => item.Count);
                var buckets = new[]
                {
                    new ClaimAgingBucket("0-7 days", counts.GetValueOrDefault(0)),
                    new ClaimAgingBucket("8-14 days", counts.GetValueOrDefault(1)),
                    new ClaimAgingBucket("15-30 days", counts.GetValueOrDefault(2)),
                    new ClaimAgingBucket("31+ days", counts.GetValueOrDefault(3))
                };
                return new ClaimAgingMarketResult(
                    group.Key,
                    buckets.Sum(bucket => bucket.ClaimCount),
                    buckets);
            })
            .ToArray();

        return new ClaimsAgingReportData(
            markets.Sum(market => market.TotalOpenClaims),
            markets);
    }

    public async Task<ClaimsPerformanceReportData> GetPerformanceReportAsync(
        IReadOnlyCollection<MarketCode> marketCodes,
        DateTimeOffset? fromDateUtc,
        DateTimeOffset? toDateExclusiveUtc,
        CancellationToken cancellationToken)
    {
        var claims = dbContext.Claims
            .AsNoTracking()
            .Where(BuildMarketPredicate(marketCodes));
        var decisions = claims.SelectMany(claim => claim.StatusHistory
            .Where(history =>
                (history.NewStatus == ClaimStatus.Approved || history.NewStatus == ClaimStatus.Declined) &&
                history.ChangedByStaffUserId != null &&
                (!fromDateUtc.HasValue || history.ChangedAtUtc >= fromDateUtc.Value) &&
                (!toDateExclusiveUtc.HasValue || history.ChangedAtUtc < toDateExclusiveUtc.Value))
            .Select(history => new
            {
                OfficerId = history.ChangedByStaffUserId!.Value,
                history.NewStatus,
                ProcessingDays = EF.Functions.DateDiffDay(claim.ReportedAtUtc, history.ChangedAtUtc)
            }));
        var grouped = await decisions
            .GroupBy(item => item.OfficerId)
            .Select(group => new
            {
                OfficerId = group.Key,
                DecisionCount = group.Count(),
                ApprovedCount = group.Count(item => item.NewStatus == ClaimStatus.Approved),
                DeclinedCount = group.Count(item => item.NewStatus == ClaimStatus.Declined),
                TotalProcessingDays = group.Sum(item => (long)item.ProcessingDays)
            })
            .OrderByDescending(item => item.DecisionCount)
            .ThenBy(item => item.OfficerId)
            .ToArrayAsync(cancellationToken);

        var officerIds = grouped.Select(item => item.OfficerId).ToArray();
        var officerNames = officerIds.Length == 0
            ? new Dictionary<Guid, string>()
            : (await dbContext.StaffUsers
                .AsNoTracking()
                .Where(staffUser => officerIds.Contains(staffUser.Id))
                .Select(staffUser => new { staffUser.Id, staffUser.Name })
                .ToArrayAsync(cancellationToken))
                .ToDictionary(item => item.Id, item => item.Name.FullName);
        var officers = grouped
            .Select(item => new OfficerDecisionPerformance(
                item.OfficerId,
                officerNames[item.OfficerId],
                item.DecisionCount,
                item.ApprovedCount,
                item.DeclinedCount,
                item.DecisionCount == 0 ? 0m : (decimal)item.TotalProcessingDays / item.DecisionCount))
            .ToArray();

        return new ClaimsPerformanceReportData(
            officers.Sum(officer => officer.DecisionCount),
            officers);
    }

    public async Task<ClaimsSlaReportData> GetSlaReportAsync(
        IReadOnlyCollection<MarketCode> marketCodes,
        DateTimeOffset? fromDateUtc,
        DateTimeOffset? toDateExclusiveUtc,
        DateTimeOffset asOfUtc,
        int targetDays,
        CancellationToken cancellationToken)
    {
        var openClaims = BuildFilteredClaimsQuery(marketCodes, fromDateUtc, toDateExclusiveUtc)
            .Where(claim =>
                claim.Status != ClaimStatus.Closed &&
                claim.Status != ClaimStatus.Settled &&
                claim.Status != ClaimStatus.Declined);
        var rows = await openClaims
            .Select(claim => new
            {
                claim.Policy.MarketCode,
                AgeDays = EF.Functions.DateDiffDay(claim.ReportedAtUtc, asOfUtc)
            })
            .GroupBy(item => item.MarketCode)
            .Select(group => new
            {
                MarketCode = group.Key,
                Total = group.Count(),
                WithinTarget = group.Count(item => item.AgeDays <= targetDays)
            })
            .ToArrayAsync(cancellationToken);

        var markets = rows
            .OrderBy(item => item.MarketCode)
            .Select(item =>
            {
                var breached = item.Total - item.WithinTarget;
                var breachRate = item.Total == 0
                    ? 0m
                    : decimal.Round((decimal)breached / item.Total * 100m, 2);
                return new ClaimSlaMarketResult(
                    item.MarketCode.Value,
                    item.Total,
                    item.WithinTarget,
                    breached,
                    breachRate);
            })
            .ToArray();

        return new ClaimsSlaReportData(
            markets.Sum(market => market.TotalOpenClaims),
            markets.Sum(market => market.WithinTarget),
            markets.Sum(market => market.Breached),
            markets);
    }

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
