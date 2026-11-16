# Security policy

## Reporting a vulnerability

Please report vulnerabilities privately through
[GitHub's private vulnerability reporting](https://github.com/jonnymuir/Heliograph.Umbraco/security/advisories/new),
not in a public issue or pull request. Include the version, what an attacker can do, and the steps
to reproduce it.

This is a solo-maintained project, so reports are answered as soon as possible rather than against
a fixed timescale. Fixes are credited in the advisory unless you'd rather not be named.

## Supported versions

Only the latest release receives security fixes.

## Scope

In scope: the published `Heliograph.Umbraco` package, in particular anything that lets someone
without backoffice access read or change templates or previews, leaks one recipient's data into
another's correspondence, or lets merged data inject markup into rendered output.

Out of scope: the reference app and its demo accounts, which never ship, and how a site delivers
rendered output. Report rendering defects in the engine to
[Heliograph](https://github.com/jonnymuir/Heliograph/security/advisories/new).
