namespace Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;

public sealed class ClaimLifecycleNotFoundException(string claimReference)
    : Exception($"Claim '{claimReference}' was not found.");

public sealed class ClaimLifecycleAccessDeniedException()
    : Exception("The current staff user is not authorized to access this claim.");

public sealed class ClaimLifecycleActorNotFoundException(Guid actorId)
    : Exception($"Claim lifecycle actor '{actorId}' was not found or is inactive.");
