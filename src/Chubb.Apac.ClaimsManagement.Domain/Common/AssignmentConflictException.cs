namespace Chubb.Apac.ClaimsManagement.Domain.Common;

public sealed class AssignmentConflictException(string message) : Exception(message);
