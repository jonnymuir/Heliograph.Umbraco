using Heliograph.Library;
using Microsoft.Extensions.DependencyInjection;
using Wayfinder.Engine.Abstractions;
using Wayfinder.Models.ServiceDesign.SupportSystems;

namespace Heliograph.Umbraco.IntegrationTests;

/// <summary>What someone who presses Run gets: the decision letter published, and the licence blueprint that sends it.</summary>
[Collection(StartedReferenceSite.Name)]
public class ReferenceSiteTests(ReferenceAppFactory site)
{
    [Fact]
    public async Task The_decision_letter_is_published_with_its_sample_recipients()
    {
        var library = site.Services.GetRequiredService<CorrespondenceLibrary>();

        var published = await Eventually(() => library.ReadVersionAsync("decision-letter"));

        published!.Version.Should().Be(1);
        (await library.Dictionaries.SamplesAsync("decision-letter")).Should().BeEquivalentTo("approved", "approved-dangerous-props", "refused");
    }

    [Fact]
    public async Task The_licence_blueprint_is_there_and_calls_Heliograph_to_send_the_letter()
    {
        var store = site.Services.GetRequiredService<IServiceBlueprintSourceStore>();

        var blueprint = await Eventually(() => store.LoadAsync("apply-for-a-juggling-event-licence"));

        blueprint!.ValidateSupportSystemActions().Should().BeEmpty();
        SupportSystemRegistry.FindCapability("heliograph", "send-correspondence").Should().NotBeNull();
    }

    /// <summary>The site seeds once Umbraco has started, which can be a moment after the first request is answered.</summary>
    private static async Task<T?> Eventually<T>(Func<Task<T?>> read)
        where T : class
    {
        for (var tries = 0; tries < 240; tries++)
        {
            if (await read() is { } found)
            {
                return found;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("The reference site did not seed within a minute.");
    }
}
