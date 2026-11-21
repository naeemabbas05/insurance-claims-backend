namespace Chubb.Apac.ClaimsManagement.Infrastructure.Integration;

internal sealed class KafkaUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
