using Heliograph.Library;
using Microsoft.Extensions.DependencyInjection;
using Wayfinder.Engine.Abstractions;
using Wayfinder.Models.ServiceDesign.SupportSystems;

namespace Heliograph.Umbraco.IntegrationTests;

/// <summary>What someone who presses Run gets, once the site has seeded (the factory waits for it): the decision letter published, and the licence blueprint that sends it.</summary>
[Collection(StartedReferenceSite.Name)]
public class ReferenceSiteTests(ReferenceAppFactory site)
{
    [Fact]
    public async Task The_decision_letter_is_published_with_its_sample_recipients()
    {
        var library = site.Services.GetRequiredService<CorrespondenceLibrary>();

        var published = await library.ReadVersionAsync("decision-letter");

        published!.Version.Should().Be(1);
        (await library.Dictionaries.SamplesAsync("decision-letter")).Should().BeEquivalentTo("approved", "approved-dangerous-props", "refused");
    }

    [Fact]
    public async Task The_licence_blueprint_is_there_and_calls_Heliograph_to_send_the_letter()
    {
        var store = site.Services.GetRequiredService<IServiceBlueprintSourceStore>();

        var blueprint = await store.LoadAsync("apply-for-a-juggling-event-licence");

        blueprint!.ValidateSupportSystemActions().Should().BeEmpty();
        SupportSystemRegistry.FindCapability("heliograph", "send-correspondence").Should().NotBeNull();
    }

    [Fact]
    public async Task A_new_dictionary_can_start_from_the_licence_blueprints_fields()
    {
        var library = site.Services.GetRequiredService<CorrespondenceLibrary>();
        var blueprint = await site.Services.GetRequiredService<IServiceBlueprintSourceStore>().LoadAsync("apply-for-a-juggling-event-licence");

        var point = (await library.Dictionaries.Editing.StartingPointsAsync()).Should().ContainSingle(p => p.Name == blueprint!.DisplayName).Subject;
        var fields = FieldDictionary.Parse(point.Json).Fields.ToDictionary(f => f.Key);

        fields["applicantName"].Name.Should().Be("Applicant name");
        fields["eventDate"].Type.Should().Be(FieldType.Date);
        fields["numberOfJugglers"].Type.Should().Be(FieldType.Number);
        fields["dangerousProps"].Type.Should().Be(FieldType.YesNo, "its options are Yes and No");
        fields["licenceApproved"].Type.Should().Be(FieldType.YesNo, "it is a calculated comparison");
    }

    [Fact]
    public async Task A_dictionary_and_its_samples_are_kept_in_the_database_from_the_workbench()
    {
        var library = site.Services.GetRequiredService<CorrespondenceLibrary>();

        library.Dictionaries.Editing.CanEdit.Should().BeTrue();
        (await library.Dictionaries.Editing.SaveAsync("kept-here", """{ "fields": [ { "name": "Member name", "key": "memberName", "type": "text" } ] }""")).Saved.Should().BeTrue();
        (await library.Dictionaries.Editing.SaveSampleAsync("kept-here", "alex", """{ "memberName": "Alex Example" }""")).Saved.Should().BeTrue();
        (await library.Dictionaries.SamplesAsync("kept-here")).Should().Equal("alex");

        await library.Dictionaries.Editing.DeleteSampleAsync("kept-here", "alex");

        (await library.Dictionaries.SamplesAsync("kept-here")).Should().BeEmpty();
        (await library.Dictionaries.SheetAsync("kept-here"))!.Fields.Should().ContainSingle();
    }
}
