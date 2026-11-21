using Chubb.Apac.ClaimsManagement.Domain.Common;
using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Domain.Entities;

public sealed class ClaimAssignment : Entity
{
    private ClaimAssignment()
    {
        StaffUser = null!;
    }

    internal ClaimAssignment(
        Guid claimId,
        StaffUser staffUser,
        AssignmentRole role,
        DateTimeOffset assignedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(staffUser);
        DomainRule.Require(claimId != Guid.Empty, "Claim identifier cannot be empty.");
        DomainRule.Require(Enum.IsDefined(role), "Assignment role is invalid.");
        DomainRule.Require(staffUser.IsActive, "Inactive staff cannot be assigned to a claim.");

        ClaimId = claimId;
        StaffUserId = staffUser.Id;
        StaffUser = staffUser;
        Role = role;
        AssignedAtUtc = DomainRule.Utc(assignedAtUtc, nameof(assignedAtUtc));
    }

    public Guid ClaimId { get; private set; }

    public Guid StaffUserId { get; private set; }

    public StaffUser StaffUser { get; private set; }

    public AssignmentRole Role { get; private set; }

    public DateTimeOffset AssignedAtUtc { get; private set; }

    public DateTimeOffset? UnassignedAtUtc { get; private set; }

    public bool IsActive => UnassignedAtUtc is null;

    internal void Unassign(DateTimeOffset unassignedAtUtc)
    {
        if (!IsActive)
        {
            throw new AssignmentConflictException("Claim assignment is already inactive.");
        }
        DomainRule.Utc(unassignedAtUtc, nameof(unassignedAtUtc));
        DomainRule.Require(unassignedAtUtc >= AssignedAtUtc, "Unassignment cannot precede assignment.");
        UnassignedAtUtc = unassignedAtUtc;
    }
}
