namespace Chubb.Apac.ClaimsManagement.Application.Claims.Submit;

public sealed class MarketNotConfiguredException(string marketCode)
    : Exception($"Market '{marketCode}' is not configured for claims.");

public sealed class PolicyNotFoundException(string policyNumber, string marketCode)
    : Exception($"Policy '{policyNumber}' was not found in market '{marketCode}'.");

public sealed class IdempotencyKeyConflictException()
    : Exception("The idempotency key has already been used with a different claim submission.");
