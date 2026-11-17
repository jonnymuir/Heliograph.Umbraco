using Heliograph.Library;
using Heliograph.Umbraco.Stores;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Wayfinder.Engine.Abstractions;
using Wayfinder.Models.ServiceDesign;

namespace Heliograph.Umbraco.ReferenceApp;

/// <summary>
/// Demo only. On first start, puts the decision letter in the library with its dictionary and sample recipients and
/// publishes it, and adds the licence blueprint that sends it, so there is something to try at once.
/// </summary>
/// <remarks>
/// On a fresh database Umbraco can still be installing, and the packages' tables still being made, when it says it has
/// started. So this waits until Umbraco is running, then tries until the seed goes in, rather than trying once at
/// start-up and quietly doing nothing. <see cref="Seeded"/> finishes when it has, or fails with why it could not.
/// </remarks>
public sealed partial class ReferenceSeeder(
    IServiceProvider services, IWebHostEnvironment environment, IRuntimeState runtime, ILogger<ReferenceSeeder> logger)
    : BackgroundService
{
    private static readonly TimeSpan Pause = TimeSpan.FromSeconds(1);
    private const int Attempts = 120;
    private readonly TaskCompletionSource _seeded = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Finishes once the demo is seeded; fails with the last reason if it never could be.</summary>
    public Task Seeded => _seeded.Task;
    private const string Letter = "decision-letter";
    private const string Blueprint = "apply-for-a-juggling-event-licence";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                if (runtime.Level == RuntimeLevel.Run)
                {
                    using var scope = services.CreateScope();
                    await SeedLetterAsync(scope.ServiceProvider, stoppingToken);
                    await SeedBlueprintAsync(scope.ServiceProvider, stoppingToken);
                    _seeded.TrySetResult();
                    return;
                }
            }
            catch (Exception problem) when (problem is not OperationCanceledException && attempt < Attempts)
            {
                LogNotYet(logger, attempt, problem.Message);
            }
            catch (Exception problem) when (problem is not OperationCanceledException)
            {
                LogNotSeeded(logger, problem);
                _seeded.TrySetException(problem);
                return;
            }

            await Task.Delay(Pause, stoppingToken);
        }

        _seeded.TrySetCanceled(stoppingToken);
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
            throw new InvalidOperationException($"The licence blueprint was not saved: {saved}");
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded the decision letter: published as version {Version}.")]
    private static partial void LogSeeded(ILogger logger, int version);

    [LoggerMessage(Level = LogLevel.Information, Message = "The demo could not be seeded yet (attempt {Attempt}): {Reason}")]
    private static partial void LogNotYet(ILogger logger, int attempt, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "The demo was not seeded.")]
    private static partial void LogNotSeeded(ILogger logger, Exception problem);
}
