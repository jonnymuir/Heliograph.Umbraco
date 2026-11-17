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
the wiring. Like the engine, it includes no word processor: the backoffice hosts the workbench,
where the email and the text are written and the document is brought in from the author's own
word processor.

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
| `src/Heliograph.Umbraco` | The publishable package. Its composer wires everything, so a site adds nothing to `Program.cs`: correspondence and dictionaries in the Umbraco database (`Stores/`, `Persistence/`), the authoring API from `Heliograph.Api` at `/heliograph/api`, the Correspondence entry under Settings hosting the workbench from `Heliograph.Workbench`, and the `CorrespondenceAdmin` policy. |
| `src/Heliograph.Umbraco.Client` | The backoffice client (TypeScript, Lit, Vite). Built into `src/Heliograph.Umbraco/wwwroot/dist`, which is not committed, and packed as static web assets. |
| `src/Heliograph.Umbraco.ReferenceApp` | An Umbraco 17 site with this package and Wayfinder.Umbraco (through its starter kit). Seeds the decision letter and a licence blueprint whose stage sends it through the `heliograph` support system, the worked example of the join. What the booted-site tests start. Never ships. See [docs/skills/reference-site](docs/skills/reference-site/SKILL.md). |
| `src/Heliograph.Umbraco.AppHost` | Aspire host: the reference site and Mailpit, a local mailbox. VS Code's Run and Debug has it and the standalone site, as Wayfinder.Umbraco's does. |
| `tests/Heliograph.Umbraco.Tests` | Fast behavioural tests, no site started (xUnit, AwesomeAssertions, Moq for Umbraco's interfaces). |
| `tests/Heliograph.Umbraco.IntegrationTests` | Tests that need a started site, sharing one start. Put a test here only when the behaviour comes from the site being whole. |

Still to come: the MCP endpoint through the sign-in package shared with Wayfinder.Umbraco, uSync,
and a check before sending that a blueprint supplies every field a correspondence needs.

## Beside Wayfinder.Umbraco

A site will often have both packages, so this one is built to look and work like its sister.
Before adding something Wayfinder.Umbraco already has, read how it does it and do the same:

- **Where it sits.** Correspondence is a menu item in the Settings section's Advanced menu that
  opens a root workspace, exactly as Blueprints is. No section of its own.
- **How it is wired.** A composer and no `Program.cs` lines. Anything a site must choose is an
  option under the `Heliograph` configuration section, as Wayfinder's is under `Wayfinder`.
- **Who gets in.** `Heliograph:CorrespondenceAdmin` mirrors `Wayfinder:BlueprintsAdmin`:
  members of `AdminGroupAliases`, Administrators by default.
- **Names.** `Heliograph.*` aliases where Wayfinder has `Wayfinder.*`; the API is
  `/umbraco/management/api/v1/heliograph/...`; the MCP endpoint will be
  `/heliograph/correspondence-authoring/mcp`.
- **Agents.** One sign-in package shared by both (decided in Heliograph's brief), and a
  "Connect" panel on the screen like Wayfinder's "Connect a design partner".
- **Guides are skills.** A guide for people who use the package goes in
  `docs/skills/<name>/SKILL.md`, as in the Heliograph repo, so it is also what an agent reads.

## Build and test

```bash
# The backoffice client must be built before the site, or the screen has nothing to load.
cd src/Heliograph.Umbraco.Client && npm ci && npm run build && cd ../..

dotnet build Heliograph.Umbraco.slnx
dotnet test Heliograph.Umbraco.slnx
node scripts/check-policies.mjs
node scripts/check-doc-links.mjs
```

The reference site installs itself into its own SQLite database on first start:

```bash
dotnet run --project src/Heliograph.Umbraco.ReferenceApp
# https://localhost:44398/umbraco, admin@example.test / Heliograph123!
```

The backoffice only signs in over HTTPS. `Program.cs` gives Umbraco a fresh image-signing key on
each run, so Umbraco never writes one into `appsettings.json`.

## Releasing

The version lives in `src/Heliograph.Umbraco/Heliograph.Umbraco.csproj`. A tag `v<version>` runs
`.github/workflows/package-release.yml`: it checks the tag against that version, runs every
suite, and publishes to nuget.org through Trusted Publishing, then creates the GitHub release. A
version with a hyphen is a prerelease. To publish one: bump the version in a PR, merge it, then
tag the merge commit. Nothing stable has been released.

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
- **The DAST and performance workflows are still to add.** There is now a reference site to scan
  and load; they come with the first screens that take input.
