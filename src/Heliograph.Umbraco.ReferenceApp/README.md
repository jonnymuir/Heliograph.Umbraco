# Heliograph.Umbraco.ReferenceApp

An Umbraco 17 site with Heliograph.Umbraco installed and nothing else. It proves the package works
in a real site, and it is what the booted-host tests start. It never ships.

```bash
cd src/Heliograph.Umbraco.Client && npm ci && npm run build && cd ../..
dotnet run --project src/Heliograph.Umbraco.ReferenceApp
```

It installs itself into its own SQLite database on first start. Then open
`https://localhost:44398/umbraco` and sign in as `admin@example.test` with `Heliograph123!`.
**Settings → Advanced → Correspondence** is the package's screen.

The sign-in above is for this site on a developer's machine only. To start again from nothing,
delete `umbraco/Data`.
