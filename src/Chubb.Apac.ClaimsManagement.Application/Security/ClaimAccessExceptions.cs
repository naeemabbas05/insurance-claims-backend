namespace Chubb.Apac.ClaimsManagement.Application.Security;

public sealed class ClaimAccessNotFoundException()
    : Exception("The requested claim was not found.");

public sealed class ClaimAccessDeniedException()
    : Exception("The current identity is not authorized to access this claim.");
