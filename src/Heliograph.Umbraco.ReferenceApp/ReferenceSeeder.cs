using Heliograph.Library;
using Heliograph.Umbraco.Stores;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Wayfinder.Engine.Abstractions;
using Wayfinder.Models.ServiceDesign;

namespace Heliograph.Umbraco.ReferenceApp;

/// <summary>
/// Demo only. On first start, puts the decision letter in the library with its dictionary and sample recipients and
/// publishes it, and adds the licence blueprint that sends it, so there is something to try at once.
/// </summary>
public sealed partial class ReferenceSeeder(
    IServiceProvider services, IWebHostEnvironment environment, IRuntimeState runtime, ILogger<ReferenceSeeder> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private const string Letter = "decision-letter";
    private const string Blueprint = "apply-for-a-juggling-event-licence";

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (runtime.Level < RuntimeLevel.Run)
        {
            return;
        }

        using var scope = services.CreateScope();
        await SeedLetterAsync(scope.ServiceProvider, cancellationToken);
        await SeedBlueprintAsync(scope.ServiceProvider, cancellationToken);
    }

    private async Task SeedLetterAsync(IServiceProvider scoped, CancellationToken ct)
    {
        var library = scoped.GetRequiredService<CorrespondenceLibrary>();
        if (await library.ReadDraftAsync(Letter, ct) is not null)
        {
            return;
        }

        var from = Path.Combine(environment.ContentRootPath, "seed", Letter);
        var dictionaries = scoped.GetRequiredService<UmbracoDictionarySource>();
        await dictionaries.SaveAsync(Letter, await File.ReadAllTextAsync(Path.Combine(from, "dictionary.json"), ct), ct);
        foreach (var sample in Directory.GetFiles(Path.Combine(from, "samples"), "*.json"))
        {
            await dictionaries.SaveSampleAsync(Letter, Path.GetFileNameWithoutExtension(sample), await File.ReadAllTextAsync(sample, ct), ct);
        }

        await library.SaveDraftAsync(Letter, new CorrespondenceContent
        {
            Name = "Decision on a juggling event licence",
            Dictionary = Letter,
            Document = await File.ReadAllBytesAsync(Path.Combine(from, "document.docx"), ct),
            Email = await File.ReadAllTextAsync(Path.Combine(from, "email.md"), ct),
            Sms = await File.ReadAllTextAsync(Path.Combine(from, "sms.txt"), ct),
        }, expectedRevision: 0, ct);
        var published = await library.PublishAsync(Letter, ct);
        LogSeeded(logger, published.Version ?? 0);
    }

    private async Task SeedBlueprintAsync(IServiceProvider scoped, CancellationToken ct)
    {
        var store = scoped.GetRequiredService<IServiceBlueprintSourceStore>();
        if (await store.LoadAsync(Blueprint, ct) is not null)
        {
            return;
        }

        var yaml = await File.ReadAllTextAsync(Path.Combine(environment.ContentRootPath, "service-blueprints", Blueprint + ".yaml"), ct);
        var saved = await store.SaveAsync(ServiceBlueprintYaml.Deserialize(yaml), expectedVersion: 0, ct);
        if (!saved.Saved)
        {
            LogBlueprintNotSeeded(logger, saved.ToString());
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded the decision letter: published as version {Version}.")]
    private static partial void LogSeeded(ILogger logger, int version);

    [LoggerMessage(Level = LogLevel.Error, Message = "The licence blueprint was not seeded: {Result}")]
    private static partial void LogBlueprintNotSeeded(ILogger logger, string result);
}
