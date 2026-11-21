namespace Chubb.Apac.ClaimsManagement.Application.Claims.Staff;

public sealed class StaffOfficerNotFoundException(Guid officerId)
    : Exception($"Officer '{officerId}' was not found or is inactive.");

public sealed class StaffClaimNotFoundException(string claimReference)
    : Exception($"Claim '{claimReference}' was not found.");

public sealed class StaffClaimAssignmentNotFoundException(Guid assignmentId)
    : Exception($"Claim assignment '{assignmentId}' was not found.");

public sealed class StaffClaimAccessDeniedException()
    : Exception("The current staff user is not authorized for this market or operation.");
