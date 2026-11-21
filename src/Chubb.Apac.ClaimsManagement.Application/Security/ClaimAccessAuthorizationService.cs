using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Application.Security;

public sealed class ClaimAccessAuthorizationService(IClaimAccessRepository repository)
    : IClaimAccessAuthorizationService
{
    public async Task AuthorizeStaffAsync(
        Guid staffUserId,
        ClaimAccessRole role,
        string marketCode,
        CancellationToken cancellationToken)
    {
        var staff = await repository.FindStaffUserAsync(staffUserId, cancellationToken);
        if (staff is null ||
            !staff.CanWorkIn(MarketCode.Create(marketCode)) ||
            !HasRequiredRole(staff.Role, role))
        {
            throw new ClaimAccessDeniedException();
        }
    }

    public async Task<ClaimAccessResult> AuthorizeAsync(
        ClaimAccessQuery query,
        CancellationToken cancellationToken)
    {
        if (!IsValidClaimReference(query.ClaimReference))
        {
            throw new ClaimAccessNotFoundException();
        }

        var claimNumber = ClaimNumber.Create(query.ClaimReference);
        var claim = await repository.FindClaimAccessDataAsync(claimNumber, cancellationToken)
            ?? throw new ClaimAccessNotFoundException();

        if (query.Role == ClaimAccessRole.Claimant)
        {
            var claimant = claim.Claimants.FirstOrDefault(item =>
                (query.ClaimantId.HasValue && item.ClaimantId == query.ClaimantId.Value) ||
                (query.EmailVerified &&
                 !string.IsNullOrWhiteSpace(query.Email) &&
                 string.Equals(item.Email, query.Email, StringComparison.OrdinalIgnoreCase)));
            return claimant is not null
                ? new ClaimAccessResult(claimant.ClaimantId)
                : throw new ClaimAccessDeniedException();
        }

        if (!query.StaffUserId.HasValue)
        {
            throw new ClaimAccessDeniedException();
        }

        var staff = await repository.FindStaffUserAsync(query.StaffUserId.Value, cancellationToken);
        if (staff is null || !staff.CanWorkIn(MarketCode.Create(claim.MarketCode)))
        {
            throw new ClaimAccessDeniedException();
        }

        if (!HasRequiredRole(staff.Role, query.Role))
        {
            throw new ClaimAccessDeniedException();
        }

        return new ClaimAccessResult(null);
    }

    private static bool IsValidClaimReference(string? value) =>
        value is { Length: 39 } &&
        value.StartsWith("CLM-", StringComparison.OrdinalIgnoreCase) &&
        char.IsAsciiLetter(value[4]) &&
        char.IsAsciiLetter(value[5]) &&
        value[6] == '-' &&
        value[7..].All(Uri.IsHexDigit);

    private static bool HasRequiredRole(StaffRole staffRole, ClaimAccessRole accessRole) =>
        accessRole switch
        {
            ClaimAccessRole.ClaimsOfficer => staffRole is
                StaffRole.ClaimsHandler or
                StaffRole.Adjuster or
                StaffRole.Assessor or
                StaffRole.Approver or
                StaffRole.FinanceOfficer,
            ClaimAccessRole.Manager => staffRole is
                StaffRole.Administrator or
                StaffRole.Approver or
                StaffRole.FinanceOfficer,
            _ => false
        };
}
