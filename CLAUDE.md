# Heliograph.Umbraco: Project Guide

## What this is

`Heliograph.Umbraco` hosts [Heliograph](https://github.com/jonnymuir/Heliograph) in Umbraco v17.
Content designers write correspondence in their own word processor, then upload, check, preview
and publish it in the backoffice. The package stores it in the Umbraco database, carries it
between environments with uSync, and renders it through the Heliograph engine for post, PDF,
email and text.

It is to Heliograph what
[Wayfinder.Umbraco](https://github.com/jonnymuir/Wayfinder.Umbraco) is to Wayfinder. The engine,
the dictionary, the compiler and the workbench live in the Heliograph repo and are consumed
here as NuGet packages. This repo adds only what is Umbraco: storage, the backoffice screens and
the wiring. Like the engine, it includes no editor.

It deliberately does **not** deliver anything, and carries no tenancy or sign-in opinion of its
own. The site sends what Heliograph renders through whichever provider it uses.

Its reference app is also where Heliograph and Wayfinder are shown working together: a service
blueprint stage sends a letter by calling Heliograph as a Wayfinder
[support system](https://github.com/jonnymuir/Wayfinder/blob/main/docs/guides/support-systems.md).

The design for both repos is in Heliograph's
[architecture](https://github.com/jonnymuir/Heliograph/blob/main/docs/architecture.md) and
[brief](https://github.com/jonnymuir/Heliograph/blob/main/docs/brief.md), which also lists the
decisions still open. Read both before proposing structure, and don't settle an open decision
without the maintainer.

Engineering standards (security, simplicity, testing, change flow, docs, writing style) are in
[jonnymuir/engineering-standards](https://github.com/jonnymuir/engineering-standards/blob/main/STANDARDS.md).
Follow them; this file adds only what is specific to this repo.

## Projects

| Project | Purpose |
|---|---|
| `src/Heliograph.Umbraco` | The publishable package. Empty until the Heliograph engine has something to host. |
| `tests/Heliograph.Umbraco.Tests` | Fast behavioural tests, no host boot (xUnit, AwesomeAssertions). |

Planned, and described in the architecture: `Heliograph.Umbraco.Client` (backoffice bundles), a
reference app that never ships, and a booted-host integration suite.

## Build and test

```bash
dotnet build Heliograph.Umbraco.slnx
dotnet test Heliograph.Umbraco.slnx
node scripts/check-policies.mjs
node scripts/check-doc-links.mjs
```

## Releasing

Not yet released. The version lives in `src/Heliograph.Umbraco/Heliograph.Umbraco.csproj`. A
publish workflow using NuGet Trusted Publishing is added with the first release; until then
`/release` has nothing to publish.

## Rules specific to this repo

- **No engine logic here.** The correspondence model, dictionary, compiler, merge and rendering
  belong in the Heliograph repo. If a change here needs to know how a template renders, the seam
  is missing upstream: add it there.
- **The package never sends.** No provider SDKs and no mail calls in `src/Heliograph.Umbraco`.
  Delivery in the reference app is sample host code, kept in the reference app.
- **The package does not depend on Wayfinder.** Only the reference app references
  Wayfinder.Umbraco, so a site can use Heliograph without it.
- **Every backoffice endpoint carries an explicit backoffice policy.** Templates and previews
  hold personal data, so nothing is anonymous, and a booted-host test proves it.
- **Correspondence contains personal data.** Never log merged data or rendered output. Sample
  content uses obviously fictional people and addresses.
- **Restore and publish use nuget.org only.** No GitHub Packages feed.
- **The DAST and performance workflows return with the reference app.** They were left out
  because there is no site to scan or load yet.
