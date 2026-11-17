using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Heliograph.Documents;
using Heliograph.Library;
using Wayfinder.Engine.Abstractions;

namespace Heliograph.Umbraco.ReferenceApp;

/// <summary>
/// Sends a correspondence when a blueprint stage asks. Host code, not part of either package: it shows how a site joins
/// Wayfinder to Heliograph and owns the delivery, which Heliograph never does.
/// </summary>
/// <remarks>
/// Wayfinder starts the call while it is moving the request on, before the request's latest values are saved. So the
/// work runs in the background once the request is saved: it is read whole, with its calculated fields, the published
/// correspondence is rendered for it, each part is delivered, and the outcome is reported to the engine the way an
/// external system's call-back would be. If Wayfinder polls first, it is given the same one result, so nothing is ever
/// sent twice.
/// </remarks>
internal sealed partial class HeliographSupportSystemClient(
    IServiceProvider services, IHostApplicationLifetime lifetime, ILogger<HeliographSupportSystemClient> logger) : ISupportSystemClient
{
    private static readonly TimeSpan SavedCheck = TimeSpan.FromMilliseconds(250);
    private readonly ConcurrentDictionary<string, Lazy<Task<SupportSystemOutcome>>> _sends = new();

    public string SupportSystemKey => HeliographSupportSystem.Key;

    public Task<SupportSystemInvocationReceipt> InvokeAsync(
        string capabilityKey, IReadOnlyDictionary<string, SupportSystemInputValue> inputs, SupportSystemInvocationContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(context);
        var call = (context.InstanceId, Text(inputs["correspondence"].RawValue) ?? "", Text(inputs["email"].RawValue));
        _sends[context.InvocationId] = new Lazy<Task<SupportSystemOutcome>>(() => SendSafelyAsync(call, lifetime.ApplicationStopping));
        _ = Task.Run(() => SendAndReportAsync(context.InstanceId, context.InvocationId, lifetime.ApplicationStopping), CancellationToken.None);
        return Task.FromResult(new SupportSystemInvocationReceipt { ExternalReference = context.InvocationId });
    }

    public async Task<SupportSystemOutcome?> CheckStatusAsync(string capabilityKey, SupportSystemInvocationReceipt receipt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return _sends.TryGetValue(receipt.ExternalReference, out var send) ? await send.Value : null;
    }

    /// <summary>An input's value as text. A value read back from the database arrives as a JSON element, not a string.</summary>
    private static string? Text(object? value) => value switch
    {
        null => null,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        JsonElement element => element.GetRawText(),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture),
    };

    /// <summary>Waits for Wayfinder to save the request, sends once, and reports the outcome to the engine.</summary>
    private async Task SendAndReportAsync(string instanceId, string invocationId, CancellationToken stopping)
    {
        for (var tries = 0; tries < 120 && !stopping.IsCancellationRequested; tries++)
        {
            if (Saved(instanceId, invocationId))
            {
                var outcome = await _sends[invocationId].Value;
                var engine = (Wayfinder.Engine.Services.ProcessManagerEngine)services.GetRequiredService<IProcessManager>();
                engine.ResolveSupportSystemOutcome(invocationId, outcome.OutcomeKey, outcome.ResultPayload);
                _sends.TryRemove(invocationId, out _);
                return;
            }

            await Task.Delay(SavedCheck, stopping);
        }
    }

    /// <summary>Whether the request, with this call recorded on it, has been saved.</summary>
    private bool Saved(string instanceId, string invocationId) =>
        services.GetRequiredService<IServiceRequestStore>().TryGet(instanceId, out var request)
        && request.SupportSystemInvocations.Any(invocation => invocation.InvocationId == invocationId);

    private async Task<SupportSystemOutcome> SendSafelyAsync((string InstanceId, string Correspondence, string? Email) call, CancellationToken ct)
    {
        try
        {
            return await SendAsync(call, ct);
        }
#pragma warning disable CA1031 // Any failure to send is the blueprint's to route on, not a reason to leave the request waiting.
        catch (Exception e)
#pragma warning restore CA1031
        {
            LogNotSent(logger, e, call.Correspondence, call.InstanceId);
            return Failed($"The correspondence could not be sent: {e.GetType().Name}.");
        }
    }

    private async Task<SupportSystemOutcome> SendAsync((string InstanceId, string Correspondence, string? Email) call, CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var library = scope.ServiceProvider.GetRequiredService<CorrespondenceLibrary>();
        if (await library.ReadVersionAsync(call.Correspondence, null, ct) is not { } published)
        {
            return Failed($"There is no published correspondence called \"{call.Correspondence}\".");
        }

        var data = CorrespondenceData.From(FieldDictionary.Parse(published.DictionaryJson), await RequestValues.ReadAsync(scope.ServiceProvider, call.InstanceId, ct));
        using var json = JsonDocument.Parse(data.ToJsonString());
        var render = await library.RenderAsync(call.Correspondence, json.RootElement, new RenderSettings(CultureInfo.GetCultureInfo("en-GB")), published.Version, ct);
        if (render.Rendered is not { } parts)
        {
            // What is missing is told to the blueprint, which routes on "failed". The data itself is never logged.
            return Failed(string.Join(" ", render.Diagnostics.Select(d => d.Message)));
        }

        await CorrespondenceDelivery.DeliverAsync(scope.ServiceProvider, new Delivery(call.InstanceId, call.Correspondence, call.Email), parts, ct);
        LogSent(logger, render.Version ?? 0, call.Correspondence, call.InstanceId);
        return new SupportSystemOutcome
        {
            OutcomeKey = "sent",
            ResultPayload = new JsonObject { ["correspondenceVersion"] = render.Version?.ToString(CultureInfo.InvariantCulture) },
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sent version {Version} of {Correspondence} for request {InstanceId}.")]
    private static partial void LogSent(ILogger logger, int version, string correspondence, string instanceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Correspondence} was not sent for request {InstanceId}.")]
    private static partial void LogNotSent(ILogger logger, Exception exception, string correspondence, string instanceId);

    private static SupportSystemOutcome Failed(string why) => new()
    {
        OutcomeKey = "failed",
        ResultPayload = new JsonObject { ["correspondenceProblem"] = why },
    };
}
