namespace Chubb.Apac.ClaimsManagement.Api.Configuration;

public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "Jwt";

    public string? Authority { get; set; }

    public string? Audience { get; set; }

    public string RoleClaimType { get; set; } = "role";
}
