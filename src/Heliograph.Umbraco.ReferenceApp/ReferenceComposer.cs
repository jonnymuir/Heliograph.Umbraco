using Heliograph.Library;
using Heliograph.Umbraco;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Wayfinder.Umbraco;
using Wayfinder.Umbraco.Starter;

namespace Heliograph.Umbraco.ReferenceApp;

/// <summary>
/// Registers the demo seeder after both packages' composers. Umbraco runs start-up handlers in the order they were
/// registered, so the seeder runs after the migrations that create Heliograph's and Wayfinder's tables.
/// </summary>
[ComposeAfter(typeof(HeliographUmbracoComposer))]
[ComposeAfter(typeof(WayfinderUmbracoComposer))]
[ComposeAfter(typeof(StarterComposer))]
public sealed class ReferenceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, ReferenceSeeder>();

        // "New dictionary" in the workbench can start from any Wayfinder service blueprint's fields.
        builder.Services.AddSingleton<IDictionaryStartingPoints, BlueprintStartingPoints>();
    }
}
