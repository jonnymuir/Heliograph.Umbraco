using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Heliograph.Umbraco.IntegrationTests;

/// <summary>What a site gets from referencing the package and writing nothing else.</summary>
[Collection(StartedReferenceSite.Name)]
public class InstalledPackageTests(ReferenceAppFactory site)
{
    [Fact]
    public async Task The_backoffice_is_told_about_the_package_and_can_load_what_it_points_at()
    {
        using var client = site.CreateClient();

        using var manifest = JsonDocument.Parse(await client.GetStringAsync(new Uri("/App_Plugins/Heliograph/umbraco-package.json", UriKind.Relative)));
        var bundle = manifest.RootElement.GetProperty("extensions").EnumerateArray().Should().ContainSingle().Subject;
        var script = await client.GetAsync(new Uri(bundle.GetProperty("js").GetString()!, UriKind.Relative));

        manifest.RootElement.GetProperty("name").GetString().Should().Be("Heliograph");
        script.StatusCode.Should().Be(HttpStatusCode.OK, "the client must be built before the site: see the reference app's README");
        (await script.Content.ReadAsStringAsync()).Should().Contain("Umb.Menu.AdvancedSettings", "Correspondence sits in the Settings section's Advanced menu, beside Blueprints");
    }

    [Fact]
    public async Task The_policy_that_guards_correspondence_is_registered_and_asks_for_more_than_a_sign_in()
    {
        var policies = site.Services.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await policies.GetPolicyAsync(HeliographUmbracoAuthorizationPolicies.CorrespondenceAdmin);

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<CorrespondenceAdminRequirement>().Should().ContainSingle();
    }
}
