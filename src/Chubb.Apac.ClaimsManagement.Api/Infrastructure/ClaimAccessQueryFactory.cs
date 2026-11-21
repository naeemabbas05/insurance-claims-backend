using System.Security.Claims;
using Chubb.Apac.ClaimsManagement.Application.Security;

namespace Chubb.Apac.ClaimsManagement.Api.Infrastructure;

internal static class ClaimAccessQueryFactory
{
    public static ClaimAccessRole GetAccessRole(ClaimsPrincipal principal) =>
        principal.IsInRole("Manager")
            ? ClaimAccessRole.Manager
            : ClaimAccessRole.ClaimsOfficer;

    public static ClaimAccessQuery Create(string claimReference, ClaimsPrincipal principal)
    {
        if (principal.IsInRole("Manager"))
        {
            return new ClaimAccessQuery(
                claimReference,
                ClaimAccessRole.Manager,
                StaffUserId: GetStaffUserId(principal));
        }

        if (principal.IsInRole("ClaimsOfficer"))
        {
            return new ClaimAccessQuery(
                claimReference,
                ClaimAccessRole.ClaimsOfficer,
                StaffUserId: GetStaffUserId(principal));
        }

        return new ClaimAccessQuery(
            claimReference,
            ClaimAccessRole.Claimant,
            ClaimantId: GetGuidClaim(principal, "claimant_id"),
            Email: principal.FindFirstValue("email"),
            EmailVerified: string.Equals(
                principal.FindFirstValue("email_verified"),
                "true",
                StringComparison.OrdinalIgnoreCase));
    }

    public static ClaimAccessQuery CreateClaimantQuery(
        string claimReference,
        ClaimsPrincipal principal) =>
        new(
            claimReference,
            ClaimAccessRole.Claimant,
            ClaimantId: GetGuidClaim(principal, "claimant_id"),
            Email: principal.FindFirstValue("email"),
            EmailVerified: HasVerifiedEmail(principal));

    public static Guid? GetStaffUserId(ClaimsPrincipal principal) =>
        GetGuidClaim(principal, "staff_user_id") ??
        GetGuidClaim(principal, "sub") ??
        GetGuidClaim(principal, ClaimTypes.NameIdentifier);

    public static bool HasVerifiedEmail(ClaimsPrincipal principal) =>
        string.Equals(
            principal.FindFirstValue("email_verified"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    private static Guid? GetGuidClaim(ClaimsPrincipal principal, string claimType) =>
        Guid.TryParse(principal.FindFirstValue(claimType), out var value) && value != Guid.Empty
            ? value
            : null;
}
