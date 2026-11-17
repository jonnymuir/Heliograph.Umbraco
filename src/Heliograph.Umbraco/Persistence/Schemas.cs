using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Heliograph.Umbraco.Persistence;

// The package's own tables. A document is kept as base64 text, so the same column type works on SQLite and SQL Server.

/// <summary>The draft of each correspondence: the copy its author is working on.</summary>
[TableName(TableName)]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public sealed class CorrespondenceDraftRow
{
    public const string TableName = "heliographCorrespondence";

    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true)]
    public int Id { get; set; }

    [Column("CorrespondenceKey")]
    [Length(200)]
    [Index(IndexTypes.UniqueNonClustered)]
    public string CorrespondenceKey { get; set; } = "";

    [Column("Name")]
    [Length(500)]
    public string Name { get; set; } = "";

    [Column("Dictionary")]
    [Length(200)]
    public string Dictionary { get; set; } = "";

    [Column("Revision")]
    public int Revision { get; set; }

    [Column("Document")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string? Document { get; set; }

    [Column("Email")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string? Email { get; set; }

    [Column("Sms")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string? Sms { get; set; }

    [Column("UpdatedUtc")]
    public DateTime UpdatedUtc { get; set; }
}

/// <summary>A published version. Never changed once written.</summary>
[TableName(TableName)]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public sealed class CorrespondenceVersionRow
{
    public const string TableName = "heliographCorrespondenceVersion";

    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true)]
    public int Id { get; set; }

    [Column("CorrespondenceKey")]
    [Length(200)]
    [Index(IndexTypes.NonClustered)]
    public string CorrespondenceKey { get; set; } = "";

    [Column("Version")]
    public int Version { get; set; }

    /// <summary>The key and the number together. Unique, so two publishes at once cannot take the same number.</summary>
    [Column("KeyAndVersion")]
    [Length(220)]
    [Index(IndexTypes.UniqueNonClustered)]
    public string KeyAndVersion { get; set; } = "";

    [Column("Name")]
    [Length(500)]
    public string Name { get; set; } = "";

    [Column("Dictionary")]
    [Length(200)]
    public string Dictionary { get; set; } = "";

    [Column("Document")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string? Document { get; set; }

    [Column("Email")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string? Email { get; set; }

    [Column("Sms")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string? Sms { get; set; }

    [Column("DictionaryJson")]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string DictionaryJson { get; set; } = "";

    [Column("PublishedUtc")]
    public DateTime PublishedUtc { get; set; }
}

/// <summary>A dictionary, or one of its sample recipients when <see cref="Sample"/> is set.</summary>
[TableName(TableName)]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public sealed class DictionaryRow
{
    public const string TableName = "heliographDictionary";

    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true)]
    public int Id { get; set; }

    [Column("Name")]
    [Length(200)]
    [Index(IndexTypes.NonClustered)]
    public string Name { get; set; } = "";

    /// <summary>Empty for the dictionary itself; the sample's name for a sample recipient.</summary>
    [Column("Sample")]
    [Length(200)]
    public string Sample { get; set; } = "";

    /// <summary>The name and the sample together, unique.</summary>
    [Column("EntryKey")]
    [Length(410)]
    [Index(IndexTypes.UniqueNonClustered)]
    public string EntryKey { get; set; } = "";

    [Column("Json")]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string Json { get; set; } = "";
}
