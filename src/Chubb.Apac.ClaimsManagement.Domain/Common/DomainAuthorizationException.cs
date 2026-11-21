namespace Chubb.Apac.ClaimsManagement.Domain.Common;

public sealed class DomainAuthorizationException(string message) : Exception(message);
