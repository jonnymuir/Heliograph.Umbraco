using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Heliograph.Umbraco.ReferenceApp;

/// <summary>
/// Turns a Wayfinder request's field values into the data a correspondence asks for, field by field, using each field's
/// type in the correspondence's dictionary: a "Yes" becomes true, "12" becomes 12, lines of an address become a list.
/// A field the request does not have is left out, so Heliograph reports it as missing instead of guessing.
/// </summary>
internal static class CorrespondenceData
{
    public static JsonObject From(FieldDictionary dictionary, IReadOnlyDictionary<string, object?> values)
    {
        var data = new JsonObject();
        foreach (var field in dictionary.Fields)
        {
            if (values.TryGetValue(field.Key, out var value) && value is not null && Convert(field, value) is { } node)
            {
                data[field.Key] = node;
            }
        }

        return data;
    }

    private static JsonNode? Convert(Field field, object value)
    {
        var text = value is JsonElement element ? (element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText()) : System.Convert.ToString(value, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return field.Type switch
        {
            FieldType.YesNo => text.Trim().ToUpperInvariant() is "TRUE" or "YES" or "Y" or "1",
            FieldType.Number or FieldType.Money => decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? JsonValue.Create(number) : null,
            FieldType.Date => DateOnly.TryParse(text, CultureInfo.InvariantCulture, out var date) ? JsonValue.Create(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : JsonValue.Create(text),
            FieldType.Address => new JsonArray([.. text.Split('\n', ',').Select(line => line.Trim()).Where(line => line.Length > 0).Select(line => (JsonNode)JsonValue.Create(line))]),
            FieldType.List => value is JsonElement { ValueKind: JsonValueKind.Array } list ? JsonNode.Parse(list.GetRawText()) : null,
            _ => JsonValue.Create(text),
        };
    }
}
