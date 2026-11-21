namespace Chubb.Apac.ClaimsManagement.Api.Configuration;

public sealed class ServiceOptions
{
    public const string SectionName = "Service";

    public string Name { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;
}
