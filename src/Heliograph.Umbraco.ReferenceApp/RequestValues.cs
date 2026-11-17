using System.Globalization;
using Wayfinder.Engine.Abstractions;
using Wayfinder.Services.Calculations;

namespace Heliograph.Umbraco.ReferenceApp;

/// <summary>Everything a request knows, read whole once Wayfinder has saved it.</summary>
internal static class RequestValues
{
    /// <summary>The request's fields, its blueprint's calculated fields, and two values the host adds: today's date and a reference.</summary>
    public static async Task<Dictionary<string, object?>> ReadAsync(IServiceProvider services, string instanceId, CancellationToken ct)
    {
        if (!services.GetRequiredService<IServiceRequestStore>().TryGet(instanceId, out var request))
        {
            return [];
        }

        var values = new Dictionary<string, object?>(request.FieldValues, StringComparer.Ordinal)
        {
            ["letterDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        };
        values.TryAdd("applicationReference", "NJF-" + instanceId[..Math.Min(8, instanceId.Length)].ToUpperInvariant());
        if (await services.GetRequiredService<IServiceBlueprintSourceStore>().LoadAsync(request.BlueprintKey, ct) is { Calculations: { } calculations })
        {
            foreach (var (name, value) in new CalculationEvaluator().EvaluateCollectingErrors(calculations, request.FieldValues).Result.Fields)
            {
                values[name] = value;
            }
        }

        return values;
    }
}
