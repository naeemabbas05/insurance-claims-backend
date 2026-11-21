using Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;
using Chubb.Apac.ClaimsManagement.Application.Integration;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Staff;

public sealed class ClaimStaffOperationsService(
    IClaimStaffOperationsRepository repository,
    IValidator<ClaimMarketPageQuery> marketPageValidator,
    IValidator<OfficerClaimsPageQuery> officerClaimsValidator,
    IValidator<OfficerWorkloadQuery> workloadValidator,
    IValidator<ClaimAssignmentHistoryQuery> assignmentHistoryValidator,
    IValidator<AssignClaimCommand> assignValidator,
    IValidator<ReassignClaimCommand> reassignValidator,
    IClaimIntegrationEventOutbox eventOutbox,
    TimeProvider timeProvider) : IClaimStaffOperationsService
{
    public async Task<PagedResult<StaffClaimListItem>> GetIncomingClaimsAsync(
        ClaimMarketPageQuery query,
        CancellationToken cancellationToken)
    {
        await marketPageValidator.ValidateAndThrowAsync(query, cancellationToken);
        var (actor, marketCode) = await GetMarketAccessAsync(
            query.ActorStaffUserId,
            query.MarketCode,
            cancellationToken);
        EnsureQueueRole(actor);
        var page = await repository.FindIncomingClaimsAsync(
            marketCode,
            GetSkip(query.Page, query.PageSize),
            query.PageSize,
            cancellationToken);
        return ToPagedResult(page, query.Page, query.PageSize);
    }

    public async Task<PagedResult<StaffClaimListItem>> GetUnassignedClaimsAsync(
        ClaimMarketPageQuery query,
        CancellationToken cancellationToken)
    {
        await marketPageValidator.ValidateAndThrowAsync(query, cancellationToken);
        var (actor, marketCode) = await GetMarketAccessAsync(
            query.ActorStaffUserId,
            query.MarketCode,
            cancellationToken);
        EnsureQueueRole(actor);
        var page = await repository.FindUnassignedClaimsAsync(
            marketCode,
            GetSkip(query.Page, query.PageSize),
            query.PageSize,
            cancellationToken);
        return ToPagedResult(page, query.Page, query.PageSize);
    }

    public async Task<PagedResult<StaffClaimListItem>> GetOfficerClaimsAsync(
        OfficerClaimsPageQuery query,
        CancellationToken cancellationToken)
    {
        await officerClaimsValidator.ValidateAndThrowAsync(query, cancellationToken);
        var (actor, marketCode) = await GetMarketAccessAsync(
            query.ActorStaffUserId,
            query.MarketCode,
            cancellationToken);
        var officer = await GetOfficerAsync(query.OfficerId, cancellationToken);
        EnsureOfficerMarketAccess(officer, marketCode);
        EnsureCanViewOtherOfficer(actor, officer);

        var page = await repository.FindOfficerClaimsAsync(
            marketCode,
            officer.Id,
            GetSkip(query.Page, query.PageSize),
            query.PageSize,
            cancellationToken);
        return ToPagedResult(page, query.Page, query.PageSize);
    }

    public async Task<OfficerWorkloadResult> GetOfficerWorkloadAsync(
        OfficerWorkloadQuery query,
        CancellationToken cancellationToken)
    {
        await workloadValidator.ValidateAndThrowAsync(query, cancellationToken);
        var (actor, marketCode) = await GetMarketAccessAsync(
            query.ActorStaffUserId,
            query.MarketCode,
            cancellationToken);
        var officer = await GetOfficerAsync(query.OfficerId, cancellationToken);
        EnsureOfficerMarketAccess(officer, marketCode);
        EnsureCanViewOtherOfficer(actor, officer);
        var workload = await repository.GetOfficerWorkloadAsync(
            marketCode,
            officer.Id,
            cancellationToken);
        return new OfficerWorkloadResult(
            officer.Id,
            officer.Name.FullName,
            marketCode.Value,
            workload.ActiveClaimCount,
            workload.ByRole);
    }

    public async Task<IReadOnlyList<ClaimAssignmentHistoryItem>> GetAssignmentHistoryAsync(
        ClaimAssignmentHistoryQuery query,
        CancellationToken cancellationToken)
    {
        await assignmentHistoryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var actor = await GetStaffUserAsync(query.ActorStaffUserId, cancellationToken);
        var claimNumber = ClaimNumber.Create(query.ClaimReference);
        var claim = await repository.FindClaimForAssignmentAsync(claimNumber, cancellationToken)
            ?? throw new StaffClaimNotFoundException(claimNumber.Value);
        if (!actor.CanWorkIn(claim.Policy.MarketCode) || !actor.CanPerform(StaffRole.ClaimsHandler))
        {
            throw new StaffClaimAccessDeniedException();
        }

        return claim.Assignments
            .OrderBy(assignment => assignment.AssignedAtUtc)
            .Select(assignment => new ClaimAssignmentHistoryItem(
                assignment.Id,
                claim.Number.Value,
                assignment.StaffUserId,
                assignment.StaffUser.Name.FullName,
                assignment.Role,
                assignment.AssignedAtUtc,
                assignment.UnassignedAtUtc,
                assignment.IsActive))
            .ToArray();
    }

    public async Task<ClaimAssignmentResult> AssignAsync(
        AssignClaimCommand command,
        CancellationToken cancellationToken)
    {
        await assignValidator.ValidateAndThrowAsync(command, cancellationToken);
        var (claim, actor) = await GetClaimAndActorAsync(
            command.ClaimReference,
            command.ActorStaffUserId,
            cancellationToken);
        var officer = await GetOfficerAsync(command.OfficerId, cancellationToken);
        var assignedAtUtc = timeProvider.GetUtcNow();
        var assignment = claim.Assign(
            officer,
            command.Role!.Value,
            actor,
            assignedAtUtc);
        AddAssignmentNotifications(claim, assignment.Id, assignedAtUtc);
        EnqueueClaimAssignedEvent(claim, assignment, assignedAtUtc);
        await repository.SaveChangesAsync(cancellationToken);
        return ToAssignmentResult(claim, assignment, actor.Id);
    }

    public async Task<ClaimAssignmentResult> ReassignAsync(
        ReassignClaimCommand command,
        CancellationToken cancellationToken)
    {
        await reassignValidator.ValidateAndThrowAsync(command, cancellationToken);
        var (claim, actor) = await GetClaimAndActorAsync(
            command.ClaimReference,
            command.ActorStaffUserId,
            cancellationToken);
        if (!claim.Assignments.Any(assignment => assignment.Id == command.AssignmentId))
        {
            throw new StaffClaimAssignmentNotFoundException(command.AssignmentId);
        }

        var newOfficer = await GetOfficerAsync(command.NewOfficerId, cancellationToken);
        var assignedAtUtc = timeProvider.GetUtcNow();
        var assignment = claim.Reassign(
            command.AssignmentId,
            newOfficer,
            actor,
            assignedAtUtc);
        AddAssignmentNotifications(claim, assignment.Id, assignedAtUtc);
        EnqueueClaimAssignedEvent(claim, assignment, assignedAtUtc);
        await repository.SaveChangesAsync(cancellationToken);
        return ToAssignmentResult(claim, assignment, actor.Id);
    }

    private async Task<(StaffUser Actor, MarketCode MarketCode)> GetMarketAccessAsync(
        Guid actorStaffUserId,
        string marketCodeValue,
        CancellationToken cancellationToken)
    {
        var actor = await GetStaffUserAsync(actorStaffUserId, cancellationToken);
        var marketCode = MarketCode.Create(marketCodeValue);
        if (!actor.CanWorkIn(marketCode))
        {
            throw new StaffClaimAccessDeniedException();
        }

        return (actor, marketCode);
    }

    private async Task<(Claim Claim, StaffUser Actor)> GetClaimAndActorAsync(
        string claimReference,
        Guid actorStaffUserId,
        CancellationToken cancellationToken)
    {
        var actor = await GetStaffUserAsync(actorStaffUserId, cancellationToken);
        var claimNumber = ClaimNumber.Create(claimReference);
        var claim = await repository.FindClaimForAssignmentAsync(claimNumber, cancellationToken)
            ?? throw new StaffClaimNotFoundException(claimNumber.Value);
        if (!actor.CanWorkIn(claim.Policy.MarketCode))
        {
            throw new StaffClaimAccessDeniedException();
        }

        return (claim, actor);
    }

    private async Task<StaffUser> GetOfficerAsync(
        Guid officerId,
        CancellationToken cancellationToken)
    {
        var officer = await repository.FindStaffUserAsync(officerId, cancellationToken);
        return officer is { IsActive: true }
            ? officer
            : throw new StaffOfficerNotFoundException(officerId);
    }

    private async Task<StaffUser> GetStaffUserAsync(
        Guid staffUserId,
        CancellationToken cancellationToken)
    {
        var staffUser = await repository.FindStaffUserAsync(staffUserId, cancellationToken);
        return staffUser is { IsActive: true }
            ? staffUser
            : throw new StaffClaimAccessDeniedException();
    }

    private static void EnsureQueueRole(StaffUser actor)
    {
        if (!actor.CanPerform(StaffRole.ClaimsHandler) && !actor.CanPerform(StaffRole.Adjuster))
        {
            throw new StaffClaimAccessDeniedException();
        }
    }

    private static void EnsureOfficerMarketAccess(StaffUser officer, MarketCode marketCode)
    {
        if (!officer.CanWorkIn(marketCode))
        {
            throw new StaffOfficerNotFoundException(officer.Id);
        }
    }

    private static void EnsureCanViewOtherOfficer(StaffUser actor, StaffUser officer)
    {
        if (actor.Id != officer.Id && !actor.CanPerform(StaffRole.ClaimsHandler))
        {
            throw new StaffClaimAccessDeniedException();
        }
    }

    private static PagedResult<StaffClaimListItem> ToPagedResult(
        StaffClaimPage page,
        int pageNumber,
        int pageSize) =>
        new(
            page.Claims.Select(ToClaimListItem).ToArray(),
            pageNumber,
            pageSize,
            page.TotalCount);

    private static StaffClaimListItem ToClaimListItem(Claim claim)
    {
        var assignment = claim.Assignments
            .Where(item => item.IsActive)
            .OrderBy(item => item.Role)
            .FirstOrDefault();

        return new StaffClaimListItem(
            claim.Number.Value,
            claim.Policy.Number.Value,
            claim.Policy.MarketCode.Value,
            claim.Type,
            claim.Status,
            claim.IncidentAtUtc,
            claim.ReportedAtUtc,
            assignment?.Role,
            assignment?.StaffUserId,
            assignment?.StaffUser.Name.FullName);
    }

    private static ClaimAssignmentResult ToAssignmentResult(
        Claim claim,
        ClaimAssignment assignment,
        Guid actorStaffUserId) =>
        new(
            assignment.Id,
            claim.Number.Value,
            assignment.StaffUserId,
            assignment.Role,
            assignment.AssignedAtUtc,
            actorStaffUserId);

    private void EnqueueClaimAssignedEvent(
        Claim claim,
        ClaimAssignment assignment,
        DateTimeOffset occurredAtUtc) =>
        eventOutbox.Enqueue(new ClaimAssignedV1(
            Guid.NewGuid(),
            occurredAtUtc,
            claim.Id,
            claim.Number.Value,
            claim.Policy.MarketCode.Value,
            assignment.Id,
            assignment.StaffUserId,
            assignment.Role.ToString()));

    private static void AddAssignmentNotifications(
        Claim claim,
        Guid assignmentId,
        DateTimeOffset occurredAtUtc)
    {
        foreach (var claimant in claim.Claimants)
        {
            claim.AddClaimantNotification(
                claimant,
                "ClaimAssigned",
                "Your claim is being processed",
                "A member of our claims team has been assigned to your claim.",
                occurredAtUtc,
                $"assignment:{assignmentId}:{claimant.Id}");
        }
    }

    private static int GetSkip(int page, int pageSize) => checked((page - 1) * pageSize);
}
