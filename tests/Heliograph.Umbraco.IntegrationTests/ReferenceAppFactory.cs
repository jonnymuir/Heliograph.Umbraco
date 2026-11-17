using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Heliograph.Umbraco.ReferenceApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Heliograph.Umbraco.IntegrationTests;

/// <summary>Every test class that needs a started site shares one, because Umbraco is slow to start.</summary>
[CollectionDefinition(Name)]
public sealed class StartedReferenceSite : ICollectionFixture<ReferenceAppFactory>
{
    public const string Name = "Started reference site";
}

/// <summary>
/// Starts the real reference site: a whole Umbraco with Heliograph.Umbraco installed as a site would install it.
/// Everything Umbraco writes on first start (its database, its models) goes to a folder made for this run and
/// removed afterwards, so nothing lands in the source tree.
/// </summary>
public sealed class ReferenceAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "heliograph-umbraco-" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Umbraco goes on installing after it starts answering, so the first requests can fail. This waits until
    /// the backoffice has answered three times running.
    /// </summary>
    public async Task InitializeAsync()
    {
        using var client = CreateClient(new() { AllowAutoRedirect = false });
        var deadline = DateTime.UtcNow.AddMinutes(3);
        var answered = 0;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var response = await client.GetAsync(new Uri("/umbraco", UriKind.Relative));
                answered = response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect ? answered + 1 : 0;
                if (answered >= 3)
                {
                    // The demo is seeded once Umbraco is running. Waiting here means a test never races it, and a
                    // seed that fails stops every test with the seeder's own reason.
                    await Services.GetRequiredService<ReferenceSeeder>().Seeded.WaitAsync(deadline - DateTime.UtcNow);
                    return;
                }
            }
            catch (HttpRequestException)
            {
                answered = 0;
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException("The reference site was not ready within 3 minutes.");
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(Path.Combine(_folder, "umbraco", "Data"));
        Directory.CreateDirectory(Path.Combine(_folder, "umbraco", "models"));

        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:umbracoDbDSN"] =
                $"Data Source={Path.Combine(_folder, "umbraco", "Data", "Umbraco.sqlite.db")};Cache=Shared;Foreign Keys=True;Pooling=True",
            ["ConnectionStrings:umbracoDbDSN_ProviderName"] = "Microsoft.Data.Sqlite",
            ["Umbraco:CMS:ModelsBuilder:ModelsDirectory"] = Path.Combine(_folder, "umbraco", "models"),
            ["Umbraco:CMS:ModelsBuilder:AcceptUnsafeModelsDirectory"] = "true",
        }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_folder))
        {
            try
            {
                Directory.Delete(_folder, recursive: true);
            }
            catch (IOException)
            {
                // SQLite can hold its file a moment after the site stops. A leftover temporary folder does no harm.
            }
        }
    }
}
