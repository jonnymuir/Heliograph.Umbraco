using Wayfinder.Models.ServiceDesign.Components;
using Wayfinder.Models.ServiceDesign.SupportSystems;

namespace Heliograph.Umbraco.ReferenceApp;

/// <summary>
/// The join between Wayfinder and Heliograph, written in the reference site as the worked example: one support system,
/// <c>heliograph</c>, with one capability, <c>send-correspondence</c>. A blueprint stage names the correspondence as a
/// fixed input and the recipient's email address as a field; the request's fields are the data.
/// </summary>
internal static class HeliographSupportSystem
{
    public const string Key = "heliograph";
    public const string SendCapability = "send-correspondence";

    /// <summary>Registers the support system. Call once, before anything reads Wayfinder's registry.</summary>
    public static void Register() => SupportSystemRegistry.Register(new SupportSystemDescriptor
    {
        Key = Key,
        DisplayName = "Heliograph correspondence",
        Description = "Renders a published correspondence from this site's Heliograph library with the request's fields as its data, and sends it: the email to the address given, the letter and the text message to the site's outbox.",
        Capabilities =
        [
            new SupportSystemCapabilityDescriptor
            {
                Key = SendCapability,
                DisplayName = "Send a correspondence",
                Inputs =
                [
                    new()
                    {
                        Key = "correspondence", Title = "Correspondence", Required = true,
                        ValueKind = ComponentPropertyValueKind.String, Format = SupportSystemInputFormats.Fixed,
                        Description = "The key of a published correspondence in Settings → Correspondence, such as decision-letter.",
                    },
                    new()
                    {
                        Key = "email", Title = "Recipient's email address", Required = true,
                        ValueKind = ComponentPropertyValueKind.String, Format = SupportSystemInputFormats.FieldRef,
                    },
                ],
                Outputs =
                [
                    new() { Key = "correspondenceVersion", Title = "Version of the correspondence sent", ValueKind = ComponentPropertyValueKind.String },
                    new() { Key = "correspondenceProblem", Title = "Why the correspondence was not sent", ValueKind = ComponentPropertyValueKind.String },
                ],
                SupportedCompletionModes = [SupportSystemCompletionMode.Webhook, SupportSystemCompletionMode.Poll],
                Outcomes =
                [
                    new() { Key = "sent", DisplayName = "Sent" },
                    new() { Key = "failed", DisplayName = "Not sent" },
                ],
            },
        ],
    });
}
