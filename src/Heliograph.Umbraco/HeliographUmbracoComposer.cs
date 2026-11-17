using Heliograph.Api;
using Heliograph.Library;
using Heliograph.Umbraco.Persistence;
using Heliograph.Umbraco.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Heliograph.Umbraco;

/// <summary>
/// Everything a site gets from referencing the package, with nothing added to its own <c>Program.cs</c>: the
/// Correspondence entry under the backoffice's Settings section with the workbench in it, correspondence and
/// dictionaries kept in the Umbraco database, the authoring API the workbench uses at <see cref="ApiPath"/>, and the
/// policy that guards it.
/// </summary>
/// <remarks>
/// It follows Wayfinder.Umbraco: the entry sits in the built-in Settings section, which a default install already
/// grants to Administrators, so there is no new section for a site to remember to switch on.
/// </remarks>
public sealed class HeliographUmbracoComposer : IComposer
{
    /// <summary>Where the authoring API is mapped: the same address the standalone workbench uses.</summary>
    public const string ApiPath = CorrespondenceAuthoringApiExtensions.DefaultPrefix;

    public void Compose(IUmbracoBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<ICorrespondenceStore, UmbracoCorrespondenceStore>();
        builder.Services.TryAddSingleton<UmbracoDictionarySource>();
        builder.Services.TryAddSingleton<IDictionarySource>(provider => provider.GetRequiredService<UmbracoDictionarySource>());
        builder.Services.AddCorrespondenceLibrary();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, HeliographMigrationHandler>();

        // The endpoints are mapped from here, so a site writes nothing. Every one needs a backoffice user in one of
        // the groups the site names; the workbench in the backoffice sends that user's token.
        builder.Services.Configure<UmbracoPipelineOptions>(options => options.AddFilter(new UmbracoPipelineFilter("Heliograph.Umbraco")
        {
            Endpoints = app => app.UseEndpoints(endpoints =>
                endpoints.MapCorrespondenceAuthoringApi(ApiPath, HeliographUmbracoAuthorizationPolicies.CorrespondenceAdmin)),
        }));

        builder.Services.AddOptions<HeliographUmbracoOptions>().BindConfiguration(HeliographUmbracoOptions.SectionName);
        builder.Services.ConfigureOptions<HeliographManagementApiConfiguration>();

        // The schemes are named because these endpoints sit outside Umbraco's Management API, which would otherwise
        // bring them: without them a site's own default sign-in (a members' cookie, say) would be asked instead.
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, CorrespondenceAdminHandler>());
        builder.Services.AddAuthorizationBuilder().AddPolicy(
            HeliographUmbracoAuthorizationPolicies.CorrespondenceAdmin,
            policy => policy
                .AddAuthenticationSchemes(global::Umbraco.Cms.Core.Constants.Security.BackOfficeAuthenticationType, "OpenIddict.Validation.AspNetCore")
                .RequireAuthenticatedUser()
                .AddRequirements(new CorrespondenceAdminRequirement()));
    }
}
