using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace Heliograph.Umbraco.Persistence;

/// <summary>Everything the package creates in the database, in order. A site's own migrations are untouched.</summary>
public sealed class HeliographMigrationPlan : PackageMigrationPlan
{
    public HeliographMigrationPlan()
        : base("Heliograph.Umbraco")
    {
    }

    protected override void DefinePlan() => To<CreateHeliographTables>("heliograph-initial");
}

/// <summary>Creates the package's tables.</summary>
public sealed class CreateHeliographTables(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        if (!TableExists(CorrespondenceDraftRow.TableName))
        {
            Create.Table<CorrespondenceDraftRow>().Do();
        }

        if (!TableExists(CorrespondenceVersionRow.TableName))
        {
            Create.Table<CorrespondenceVersionRow>().Do();
        }

        if (!TableExists(DictionaryRow.TableName))
        {
            Create.Table<DictionaryRow>().Do();
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Runs the migration plan once Umbraco has started, as Wayfinder.Umbraco does: after an unattended install, so the
/// tables are there on a new site's first start. Registered by the package's composer, so it runs before a site's own
/// start-up handlers that might read the tables.
/// </summary>
public sealed class HeliographMigrationHandler(
    IMigrationPlanExecutor executor, ICoreScopeProvider scopes, IKeyValueService keyValues, IRuntimeState runtime)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (runtime.Level < RuntimeLevel.Run)
        {
            return;
        }

        await new Upgrader(new HeliographMigrationPlan()).ExecuteAsync(executor, scopes, keyValues);
    }
}
