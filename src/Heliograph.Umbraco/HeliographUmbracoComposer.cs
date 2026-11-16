using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Heliograph.Umbraco;

/// <summary>
/// Everything a site gets from referencing the package, with nothing added to its own <c>Program.cs</c>: the
/// Correspondence entry under the backoffice's Settings section, and the policy that guards it.
/// </summary>
/// <remarks>
/// It follows Wayfinder.Umbraco: the entry sits in the built-in Settings section, which a default install already
/// grants to Administrators, so there is no new section for a site to remember to switch on.
/// </remarks>
public sealed class HeliographUmbracoComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<HeliographUmbracoOptions>().BindConfiguration(HeliographUmbracoOptions.SectionName);
        builder.Services.ConfigureOptions<HeliographManagementApiConfiguration>();

        // No sign-in scheme is named, as in Wayfinder.Umbraco: a backoffice endpoint brings the backoffice's own,
        // and an agent's token is validated by Umbraco before the handler looks at the user's groups.
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, CorrespondenceAdminHandler>());
        builder.Services.AddAuthorizationBuilder().AddPolicy(
            HeliographUmbracoAuthorizationPolicies.CorrespondenceAdmin,
            policy => policy.RequireAuthenticatedUser().AddRequirements(new CorrespondenceAdminRequirement()));
    }
}
