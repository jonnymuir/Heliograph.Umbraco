# Heliograph.Umbraco

[Heliograph](https://github.com/jonnymuir/Heliograph) for Umbraco: content designers write
correspondence in the word processor they already use, check and publish it in the backoffice,
and the site renders it for post, PDF, email and text.

Heliograph renders; it does not deliver. Your site sends what it produces through whichever
provider you use. With [Wayfinder.Umbraco](https://github.com/jonnymuir/Wayfinder.Umbraco)
installed, a service blueprint can send a letter at any stage of a journey.

> **Status:** early. Nothing is published yet. The package installs into Umbraco 17: correspondence
> and dictionaries are kept in the Umbraco database, and **Settings → Correspondence** is the
> Heliograph workbench, where authors write, check, preview and publish. See the
> [architecture](https://github.com/jonnymuir/Heliograph/blob/main/docs/architecture.md).

## Try it

Clone the repo and press Run in VS Code (**C#: Aspire (Heliograph.Umbraco.ReferenceApp)**), or:

```bash
cd src/Heliograph.Umbraco.Client && npm install && npm run build && cd ../..
dotnet run --project src/Heliograph.Umbraco.ReferenceApp
```

The reference site has Heliograph.Umbraco and Wayfinder.Umbraco together: apply for a licence,
decide as the caseworker, and Heliograph sends the decision letter. The
[reference site guide](https://github.com/jonnymuir/Heliograph.Umbraco/blob/main/docs/skills/reference-site/SKILL.md)
walks through it and shows how the two are joined.

## Licence

[MIT](https://github.com/jonnymuir/Heliograph.Umbraco/blob/main/LICENSE)
