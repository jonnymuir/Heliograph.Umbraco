using System.Globalization;
using Heliograph.Library;
using Heliograph.Umbraco.Persistence;
using Umbraco.Cms.Infrastructure.Persistence;

namespace Heliograph.Umbraco.Stores;

/// <summary>
/// Correspondence kept in the Umbraco database. A save checks the revision and writes in one statement, so two
/// people saving at once cannot both succeed, on one server or many.
/// </summary>
public sealed class UmbracoCorrespondenceStore(IUmbracoDatabaseFactory databases) : ICorrespondenceStore
{
    public Task<IReadOnlyList<CorrespondenceSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var db = databases.CreateDatabase();
        var latest = db.Fetch<CorrespondenceVersionRow>($"SELECT CorrespondenceKey, MAX(Version) AS Version FROM {CorrespondenceVersionRow.TableName} GROUP BY CorrespondenceKey")
            .ToDictionary(row => row.CorrespondenceKey, row => row.Version, StringComparer.Ordinal);
        IReadOnlyList<CorrespondenceSummary> summaries = [.. db.Fetch<CorrespondenceDraftRow>($"SELECT * FROM {CorrespondenceDraftRow.TableName} ORDER BY CorrespondenceKey")
            .Select(row => new CorrespondenceSummary(row.CorrespondenceKey, row.Name, row.Dictionary, latest.TryGetValue(row.CorrespondenceKey, out var v) ? v : null))];
        return Task.FromResult(summaries);
    }

    public Task<CorrespondenceDraft?> ReadDraftAsync(string key, CancellationToken cancellationToken = default)
    {
        using var db = databases.CreateDatabase();
        var row = db.FirstOrDefault<CorrespondenceDraftRow>($"SELECT * FROM {CorrespondenceDraftRow.TableName} WHERE CorrespondenceKey = @0", key);
        return Task.FromResult(row is null ? null : new CorrespondenceDraft(row.CorrespondenceKey, Content(row.Name, row.Dictionary, row.Document, row.Email, row.Sms), row.Revision));
    }

    public Task<int?> SaveDraftAsync(string key, CorrespondenceContent content, int expectedRevision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        using var db = databases.CreateDatabase();
        var document = content.Document is null ? null : Convert.ToBase64String(content.Document);
        if (expectedRevision == 0)
        {
            var exists = db.ExecuteScalar<int>($"SELECT COUNT(*) FROM {CorrespondenceDraftRow.TableName} WHERE CorrespondenceKey = @0", key) > 0;
            if (exists)
            {
                return Task.FromResult<int?>(null);
            }

            try
            {
                db.Insert(new CorrespondenceDraftRow
                {
                    CorrespondenceKey = key, Name = content.Name, Dictionary = content.Dictionary, Revision = 1,
                    Document = document, Email = content.Email, Sms = content.Sms, UpdatedUtc = DateTime.UtcNow,
                });
                return Task.FromResult<int?>(1);
            }
            catch (System.Data.Common.DbException)
            {
                // Someone else created it between the check and the insert: the unique key refused the second.
                return Task.FromResult<int?>(null);
            }
        }

        var changed = db.Execute(
            $"UPDATE {CorrespondenceDraftRow.TableName} SET Name = @0, Dictionary = @1, Revision = @2, Document = @3, Email = @4, Sms = @5, UpdatedUtc = @6 " +
            "WHERE CorrespondenceKey = @7 AND Revision = @8",
            content.Name, content.Dictionary, expectedRevision + 1, Nullable(document), Nullable(content.Email), Nullable(content.Sms), DateTime.UtcNow, key, expectedRevision);
        return Task.FromResult<int?>(changed == 1 ? expectedRevision + 1 : null);
    }

    public Task<PublishedCorrespondence> AddVersionAsync(
        string key, CorrespondenceContent content, string dictionaryJson, DateTimeOffset publishedAt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        using var db = databases.CreateDatabase();
        for (var attempt = 0; ; attempt++)
        {
            var version = db.ExecuteScalar<int?>($"SELECT MAX(Version) FROM {CorrespondenceVersionRow.TableName} WHERE CorrespondenceKey = @0", key) ?? 0;
            version++;
            try
            {
                db.Insert(new CorrespondenceVersionRow
                {
                    CorrespondenceKey = key, Version = version, KeyAndVersion = $"{key}#{version.ToString(CultureInfo.InvariantCulture)}",
                    Name = content.Name, Dictionary = content.Dictionary, Email = content.Email, Sms = content.Sms,
                    Document = content.Document is null ? null : Convert.ToBase64String(content.Document),
                    DictionaryJson = dictionaryJson, PublishedUtc = publishedAt.UtcDateTime,
                });
                return Task.FromResult(new PublishedCorrespondence(key, version, content, dictionaryJson, publishedAt));
            }
            catch (System.Data.Common.DbException) when (attempt < 3)
            {
                // Another publish took this number first. Take the next one.
            }
        }
    }

    public Task<PublishedCorrespondence?> ReadVersionAsync(string key, int? version = null, CancellationToken cancellationToken = default)
    {
        using var db = databases.CreateDatabase();
        var row = version is { } number
            ? db.FirstOrDefault<CorrespondenceVersionRow>($"SELECT * FROM {CorrespondenceVersionRow.TableName} WHERE CorrespondenceKey = @0 AND Version = @1", key, number)
            : db.FirstOrDefault<CorrespondenceVersionRow>($"SELECT * FROM {CorrespondenceVersionRow.TableName} WHERE CorrespondenceKey = @0 ORDER BY Version DESC", key);
        return Task.FromResult(row is null ? null : new PublishedCorrespondence(
            row.CorrespondenceKey, row.Version, Content(row.Name, row.Dictionary, row.Document, row.Email, row.Sms), row.DictionaryJson,
            new DateTimeOffset(DateTime.SpecifyKind(row.PublishedUtc, DateTimeKind.Utc))));
    }

    /// <summary>A part that is not there is written as a database null.</summary>
    private static object Nullable(string? value) => value is null ? DBNull.Value : value;

    private static CorrespondenceContent Content(string name, string dictionary, string? document, string? email, string? sms) => new()
    {
        Name = name, Dictionary = dictionary, Email = email, Sms = sms,
        Document = document is null ? null : Convert.FromBase64String(document),
    };
}
