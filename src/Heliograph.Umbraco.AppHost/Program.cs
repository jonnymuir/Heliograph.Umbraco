// The reference site's orchestrator, as Wayfinder.Umbraco's is: one command starts the site and a local mailbox, so the
// decision letter Heliograph sends arrives somewhere a person can open it. The site installs itself into its own SQLite
// file, so there is nothing else to start.
#pragma warning disable CS0618 // ResourceUrlAnnotation.DisplayOrder is marked obsolete but still orders the links.
var builder = DistributedApplication.CreateBuilder(args);

// Mailpit: an SMTP server that keeps every message and shows it in a web page. Nothing leaves the machine.
var mailpit = builder.AddContainer("mailpit", "axllent/mailpit", "v1.31")
    .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "web")
    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp");

builder.AddProject<Projects.Heliograph_Umbraco_ReferenceApp>("referenceapp", launchProfileName: "Heliograph.Umbraco.ReferenceApp")
    .WithReference(mailpit.GetEndpoint("smtp"))
    .WaitFor(mailpit)
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Host", "localhost")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Port", "1025")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__DeliveryMethod", "Network")
    .WithUrls(context =>
    {
        if (context.Urls.Select(u => u.Url).FirstOrDefault(u => u?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true) is { } https)
        {
            var root = new Uri(https).GetLeftPart(UriPartial.Authority);
            context.Urls.Add(new ResourceUrlAnnotation { Url = root + "/", DisplayText = "Reference site", DisplayOrder = 1 });
            context.Urls.Add(new ResourceUrlAnnotation { Url = root + "/umbraco/section/settings/workspace/heliograph-correspondence-root", DisplayText = "Correspondence (backoffice)", DisplayOrder = 2 });
        }
    });

builder.Build().Run();
