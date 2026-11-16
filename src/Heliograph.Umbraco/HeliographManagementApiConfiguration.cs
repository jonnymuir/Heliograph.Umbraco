using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Cms.Api.Management.OpenApi;

namespace Heliograph.Umbraco;

/// <summary>Gives Heliograph's backoffice endpoints a document of their own in the Management API's Swagger.</summary>
public sealed class HeliographManagementApiConfiguration : IConfigureOptions<SwaggerGenOptions>
{
    /// <summary>The name of the API, used in routes, the Swagger document and the security filter.</summary>
    public const string ApiName = "Heliograph";

    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc(ApiName, new OpenApiInfo { Title = "Heliograph Management API", Version = "v1" });
        options.OperationFilter<HeliographSecurityFilter>();
    }
}

/// <summary>Marks every Heliograph endpoint in Swagger as needing a backoffice sign-in.</summary>
public sealed class HeliographSecurityFilter : BackOfficeSecurityRequirementsOperationFilterBase
{
    protected override string ApiName => HeliographManagementApiConfiguration.ApiName;
}
