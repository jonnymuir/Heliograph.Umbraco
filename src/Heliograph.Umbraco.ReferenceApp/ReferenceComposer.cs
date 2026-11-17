using Heliograph.Library;
using Heliograph.Umbraco;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Wayfinder.Umbraco;
using Wayfinder.Umbraco.Starter;

namespace Heliograph.Umbraco.ReferenceApp;

/// <summary>
/// The reference site's own wiring: the demo seeder, and blueprints as starting points for a new dictionary. It composes
/// after both packages so their services are there to use.
/// </summary>
[ComposeAfter(typeof(HeliographUmbracoComposer))]
[ComposeAfter(typeof(WayfinderUmbracoComposer))]
[ComposeAfter(typeof(StarterComposer))]
public sealed class ReferenceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<ReferenceSeeder>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<ReferenceSeeder>());

        // "New dictionary" in the workbench can start from any Wayfinder service blueprint's fields.
        builder.Services.AddSingleton<IDictionaryStartingPoints, BlueprintStartingPoints>();
    }
}
