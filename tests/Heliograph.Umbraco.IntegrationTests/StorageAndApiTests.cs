using System.Globalization;
using System.Net;
using System.Text.Json;
using Heliograph.Library;
using Heliograph.Umbraco.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Heliograph.Umbraco.IntegrationTests;

/// <summary>Correspondence kept in a real Umbraco database, and the API in front of it, as a site gets them.</summary>
[Collection(StartedReferenceSite.Name)]
public class StorageAndApiTests(ReferenceAppFactory site)
{
    private const string Dictionary = """{ "fields": [ { "name": "Member name", "key": "memberName", "type": "text" } ] }""";

    [Fact]
    public async Task A_correspondence_is_saved_published_and_rendered_from_the_Umbraco_database()
    {
        var dictionaries = site.Services.GetRequiredService<UmbracoDictionarySource>();
        await dictionaries.SaveAsync("storage-test", Dictionary);
        await dictionaries.SaveSampleAsync("storage-test", "alex", """{ "memberName": "Alex Applicant" }""");
        var library = site.Services.GetRequiredService<CorrespondenceLibrary>();
        var key = "storage-test-" + Guid.NewGuid().ToString("N")[..8];
        var content = new CorrespondenceContent { Name = "Storage test", Dictionary = "storage-test", Sms = "Hi ((Member name))." };

        (await library.SaveDraftAsync(key, content, 0)).Revision.Should().Be(1);
        (await library.SaveDraftAsync(key, content, 0)).Saved.Should().BeFalse("a second save from revision 0 is out of date");
        (await library.PublishAsync(key)).Version.Should().Be(1);
        using var data = JsonDocument.Parse((await library.Dictionaries.SampleAsync("storage-test", "alex"))!);
        var render = await library.RenderAsync(key, data.RootElement, new RenderSettings(CultureInfo.GetCultureInfo("en-GB")));

        (render.Version, render.Rendered!.Sms!.Text).Should().Be((1, "Hi Alex Applicant."));
        (await library.ListAsync()).Should().Contain(summary => summary.Key == key && summary.LatestVersion == 1);
    }

    [Fact]
    public async Task A_document_survives_the_round_trip_through_the_database_byte_for_byte()
    {
        var store = site.Services.GetRequiredService<ICorrespondenceStore>();
        var bytes = Enumerable.Range(0, 5000).Select(i => (byte)(i % 251)).ToArray();
        var key = "bytes-" + Guid.NewGuid().ToString("N")[..8];

        await store.SaveDraftAsync(key, new CorrespondenceContent { Name = "Bytes", Dictionary = "x", Document = bytes }, 0);

        (await store.ReadDraftAsync(key))!.Content.Document.Should().Equal(bytes);
    }

    [Fact]
    public async Task Nobody_reaches_the_authoring_API_without_signing_in_to_the_backoffice()
    {
        using var client = site.CreateClient();

        var response = await client.GetAsync(new Uri("/heliograph/api/correspondence", UriKind.Relative));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_word_processor_with_a_made_up_link_is_refused_and_one_without_a_sign_in_gets_no_link()
    {
        using var client = site.CreateClient();

        var forged = await client.GetAsync(new Uri("/heliograph/webdav/not-a-real-token/letter.docx", UriKind.Relative));
        var link = await client.PostAsync(new Uri("/heliograph/api/correspondence/decision-letter/document/edit-link", UriKind.Relative), null);

        forged.StatusCode.Should().Be(HttpStatusCode.NotFound);
        link.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_workbench_the_backoffice_loads_is_served()
    {
        using var client = site.CreateClient();

        var response = await client.GetAsync(new Uri("/_content/Heliograph.Workbench/heliograph-workbench.js", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
