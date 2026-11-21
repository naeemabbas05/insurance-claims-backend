namespace Chubb.Apac.ClaimsManagement.Domain.Common;

public static class DomainRule
{
    public static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new DomainRuleViolationException(message);
        }
    }

    public static string RequiredText(string? value, string name, int maximumLength)
    {
        var trimmed = value?.Trim();
        Require(!string.IsNullOrWhiteSpace(trimmed), $"{name} is required.");
        Require(trimmed!.Length <= maximumLength, $"{name} cannot exceed {maximumLength} characters.");
        return trimmed;
    }

    public static DateTimeOffset Utc(DateTimeOffset value, string name)
    {
        Require(value.Offset == TimeSpan.Zero, $"{name} must be a UTC timestamp.");
        return value;
    }
}
