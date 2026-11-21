using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Chubb.Apac.ClaimsManagement.Api.Infrastructure;

public sealed class ConfigureSwaggerOptions(
    IApiVersionDescriptionProvider apiVersionDescriptionProvider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "Chubb APAC Claims Management API",
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated
                    ? "This API version has been deprecated."
                    : "Chubb APAC Claims Management backend API."
            });
        }
    }
}
