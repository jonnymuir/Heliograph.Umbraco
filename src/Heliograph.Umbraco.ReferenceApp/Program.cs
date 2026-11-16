using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Local overrides, never committed.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Umbraco makes a key for signing image addresses on first start and writes it into appsettings.json, where it
// would be committed by accident. Given one here, made fresh for each run, it writes nothing. A real site keeps
// its own key in its secrets.
builder.Configuration["Umbraco:CMS:Imaging:HMACSecretKey"] ??= Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

// Heliograph.Umbraco needs nothing here: its composer, found by AddComposers, adds the Correspondence entry to the
// backoffice and the policy that guards it.
builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

var app = builder.Build();

await app.BootUmbracoAsync();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();

/// <summary>The reference site's entry point, named so the booted-host tests can start it.</summary>
public partial class Program;
