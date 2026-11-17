using System.Security.Cryptography;
using Heliograph.Documents;
using Heliograph.Pdf.LibreOffice;
using Heliograph.Umbraco.ReferenceApp;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Wayfinder.Engine.Abstractions;

// Wayfinder reads its support systems once, so Heliograph's is registered before anything else starts.
HeliographSupportSystem.Register();

var builder = WebApplication.CreateBuilder(args);

// Local overrides, never committed.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Umbraco makes a key for signing image addresses on first start and writes it into appsettings.json, where it
// would be committed by accident. Given one here, made fresh for each run, it writes nothing.
builder.Configuration["Umbraco:CMS:Imaging:HMACSecretKey"] ??= Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

// With no mail server (a bare run rather than the Aspire host's Mailpit), Umbraco writes each email to a folder.
builder.Configuration["Umbraco:CMS:Global:Smtp:From"] ??= "licensing@njf.example";
if (string.IsNullOrEmpty(builder.Configuration["Umbraco:CMS:Global:Smtp:Host"]))
{
    var mail = Path.Combine(builder.Environment.ContentRootPath, "umbraco", "Data", "Mail");
    Directory.CreateDirectory(mail);
    builder.Configuration["Umbraco:CMS:Global:Smtp:PickupDirectoryLocation"] ??= mail;
}

// Heliograph.Umbraco and Wayfinder.Umbraco's starter wire themselves through their composers. This site adds the join
// between them (the "heliograph" support system), a route to PDF, and its demo content.
builder.Services.AddSingleton<ISupportSystemClient, HeliographSupportSystemClient>();
builder.Services.AddSingleton<IPdfConverter, LibreOfficePdfConverter>();

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

builder.Services.AddSingleton<INotificationAsyncHandler<UmbracoApplicationStartedNotification>, ReferenceSeeder>();

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

/// <summary>The reference site's entry point, named so the booted-site tests can start it.</summary>
public partial class Program;
