using Heliograph.Library;
using Heliograph.Umbraco.Persistence;
using Umbraco.Cms.Infrastructure.Persistence;

namespace Heliograph.Umbraco.Stores;

/// <summary>
/// Dictionaries and their sample recipients kept in the Umbraco database. Authors keep them from the workbench's
/// Dictionaries screen, which checks each change before it reaches this store.
/// </summary>
public sealed class UmbracoDictionarySource(IUmbracoDatabaseFactory databases) : IDictionaryStore
{
    public Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var db = databases.CreateDatabase();
        IReadOnlyList<string> names = db.Fetch<string>($"SELECT Name FROM {DictionaryRow.TableName} WHERE Sample = '' ORDER BY Name");
        return Task.FromResult(names);
    }

    public Task<string?> ReadAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult(Read(name, ""));

    public Task<IReadOnlyList<string>> ListSamplesAsync(string name, CancellationToken cancellationToken = default)
    {
        using var db = databases.CreateDatabase();
        IReadOnlyList<string> samples = db.Fetch<string>($"SELECT Sample FROM {DictionaryRow.TableName} WHERE Name = @0 AND Sample <> '' ORDER BY Sample", name);
        return Task.FromResult(samples);
    }

    public Task<string?> ReadSampleAsync(string name, string sample, CancellationToken cancellationToken = default) =>
        Task.FromResult(string.IsNullOrEmpty(sample) ? null : Read(name, sample));

    /// <summary>Adds or replaces a dictionary. The JSON is checked first, so a broken dictionary is never stored.</summary>
    public Task SaveAsync(string name, string json, CancellationToken cancellationToken = default)
    {
        FieldDictionary.Parse(json);
        Write(name, "", json);
        return Task.CompletedTask;
    }

    /// <summary>Adds or replaces one of a dictionary's made-up sample recipients, as a JSON object.</summary>
    public Task SaveSampleAsync(string name, string sample, string json, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sample);
        using (System.Text.Json.JsonDocument.Parse(json))
        {
        }

        Write(name, sample, json);
        return Task.CompletedTask;
    }

    /// <summary>Removes a sample recipient. The dictionary itself is never removed this way.</summary>
    public Task DeleteSampleAsync(string name, string sample, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(sample))
        {
            using var db = databases.CreateDatabase();
            db.Execute($"DELETE FROM {DictionaryRow.TableName} WHERE EntryKey = @0", EntryKey(name, sample));
        }

        return Task.CompletedTask;
    }

    private string? Read(string name, string sample)
    {
        using var db = databases.CreateDatabase();
        return db.FirstOrDefault<DictionaryRow>($"SELECT * FROM {DictionaryRow.TableName} WHERE EntryKey = @0", EntryKey(name, sample))?.Json;
    }

    private void Write(string name, string sample, string json)
    {
        using var db = databases.CreateDatabase();
        var key = EntryKey(name, sample);
        if (db.Execute($"UPDATE {DictionaryRow.TableName} SET Json = @0 WHERE EntryKey = @1", json, key) == 0)
        {
            db.Insert(new DictionaryRow { Name = name, Sample = sample, EntryKey = key, Json = json });
        }
    }

    private static string EntryKey(string name, string sample) => $"{name}/{sample}";
}
