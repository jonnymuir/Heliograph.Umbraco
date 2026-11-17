using System.Text.Json;
using System.Text.RegularExpressions;
using Heliograph.Library;
using Wayfinder.Engine.Abstractions;
using Wayfinder.Extensions;
using Wayfinder.Models.ServiceDesign;
using Wayfinder.Models.ServiceDesign.Components;

namespace Heliograph.Umbraco.ReferenceApp;

/// <summary>
/// Offers one ready-made dictionary per Wayfinder service blueprint for "New dictionary" in the workbench: a field for
/// every input a person fills in and every calculated field, named from its key ("applicantName" becomes
/// "Applicant name") and typed from the component. The author then adjusts it, for example making a fee money.
/// </summary>
internal sealed partial class BlueprintStartingPoints(IServiceScopeFactory scopes) : IDictionaryStartingPoints
{
    public async Task<IReadOnlyList<DictionaryStartingPoint>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopes.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IServiceBlueprintSourceStore>();
        var points = new List<DictionaryStartingPoint>();
        foreach (var summary in await store.ListAsync(cancellationToken).ConfigureAwait(false))
        {
            if (await store.LoadAsync(summary.DefinitionKey, cancellationToken).ConfigureAwait(false) is { } blueprint)
            {
                points.Add(new DictionaryStartingPoint(
                    blueprint.DisplayName,
                    $"Every field the \"{blueprint.DisplayName}\" service collects or calculates.",
                    Dictionary(blueprint)));
            }
        }

        return points;
    }

    internal static string Dictionary(ServiceBlueprint blueprint)
    {
        var fields = new List<object>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var input in blueprint.Stages.SelectMany(stage => stage.Components).GetSubmittableInputs())
        {
            if (input.FieldKey.Length > 0 && seen.Add(input.FieldKey))
            {
                fields.Add(Field(input.FieldKey, TypeOf(input)));
            }
        }

        foreach (var (key, calculated) in blueprint.Calculations?.Fields ?? new Dictionary<string, Wayfinder.Models.ServiceDesign.Calculations.ServiceBlueprintCalculationField>())
        {
            if (seen.Add(key) && calculated.Shape is null)
            {
                fields.Add(Field(key, TypeOf(calculated.ValueKind, calculated.Expr)));
            }
        }

        return JsonSerializer.Serialize(new { fields }, Indented) + "\n";
    }

    private static object Field(string key, string type) => new { name = NameFor(key), key, type };

    private static string TypeOf(InputComponent input) => input switch
    {
        DateInputComponent => "date",
        NumberInputComponent or DecimalInputComponent or SliderComponent => "number",
        BooleanComponent => "yesNo",
        RadiosComponent { Options: var options } when IsYesNo(options) => "yesNo",
        SelectComponent { Options: var options } when IsYesNo(options) => "yesNo",
        _ => "text",
    };

    // A calculation with no stated kind that compares or combines is a yes or no: "decision = 'Approve'".
    private static string TypeOf(string? valueKind, string? expression) => valueKind switch
    {
        "number" => "number",
        "boolean" => "yesNo",
        "string" => "text",
        _ => expression is not null && Comparison().IsMatch(expression) ? "yesNo" : "text",
    };

    private static bool IsYesNo(IReadOnlyList<string> options) =>
        options.Count == 2 && options.Select(o => o.Trim().ToUpperInvariant()).Order().SequenceEqual(["NO", "YES"]);

    /// <summary>"applicantName" becomes "Applicant name", as an author would write the tag.</summary>
    internal static string NameFor(string key)
    {
        var words = Words().Matches(key).Select(m => m.Value.ToLowerInvariant()).ToList();
        return words.Count == 0 ? key : char.ToUpperInvariant(words[0][0]) + string.Join(' ', words)[1..];
    }

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    [GeneratedRegex("[A-Z]?[a-z]+|[0-9]+|[A-Z]+(?![a-z])")]
    private static partial Regex Words();

    [GeneratedRegex(@"[=<>]|\b(and|or|not)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Comparison();
}
