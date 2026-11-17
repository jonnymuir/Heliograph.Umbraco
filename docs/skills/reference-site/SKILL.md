---
name: reference-site
description: How to run the Heliograph.Umbraco reference site, an Umbraco 17 site with Heliograph.Umbraco and Wayfinder.Umbraco together, and try the whole journey from a licence application to the decision letter Heliograph sends. Covers what is seeded, the backoffice workbench, and how the two packages are joined. Use when trying Heliograph in Umbraco or joining it to Wayfinder on another site.
---

# The reference site

An Umbraco 17 site with both packages installed: **Heliograph.Umbraco** for correspondence and
**Wayfinder.Umbraco** (through its starter kit) for the service. It shows them working together:
a member of the public applies for a juggling event licence, a licensing officer decides, and
Heliograph sends the decision letter as an email, a PDF and a text message.

It never ships. It installs itself into its own SQLite database on first start. Delete
`src/Heliograph.Umbraco.ReferenceApp/umbraco/Data` to start again.

## Run it

You need the .NET 10 SDK and Node.js 20 or later. LibreOffice is optional; with it, the letter is
attached to the email as a PDF. Docker is needed only for the Aspire host's mailbox.

| How | What to do |
|---|---|
| VS Code, with Aspire | Choose **C#: Aspire (Heliograph.Umbraco.ReferenceApp)** in Run and Debug and press play. The Aspire dashboard opens with links to the site, the Correspondence screen and **Mailpit**, a mailbox where every email the site sends arrives. |
| VS Code | Choose **C#: Heliograph.Umbraco.ReferenceApp (standalone)**. Emails are written as `.eml` files to `umbraco/Data/Mail` instead. |
| A terminal | `cd src/Heliograph.Umbraco.Client && npm install && npm run build && cd ../..`, then `dotnet run --project src/Heliograph.Umbraco.ReferenceApp`, and open `https://localhost:44398`. |

The backoffice sign-in is `admin@example.test` / `Heliograph123!`. It is for this site on a
developer's machine only. The backoffice only signs in over HTTPS.

## What is there when it starts

- **The decision letter**, in **Settings → Correspondence**: a Word document, an email and a text
  message, its dictionary and three sample recipients, published as version 1.
- **Apply for a juggling event licence**, a Wayfinder service blueprint, listed on the home page.
- **Request a callback** and the rest of Wayfinder's starter kit: a home page, a work queue, and
  a demo caseworker you sign in as with one button.

## Try the journey

1. **Apply.** On the home page open **Apply for a juggling event licence**, fill it in with a
   made-up name and an email address, and send it.
2. **Decide.** Open **Work queue**, sign in as the caseworker, pick up the application and press
   **Review**. Choose **Approve** with a licence number, fee and due date, or **Refuse** with a
   reason and a review date, and press **Send the decision letter**.
3. **See what was sent.** The page moves on to **Decision letter sent**, with the version of the
   letter that was used. Open Mailpit (or `umbraco/Data/Mail`): the email is there with the letter
   attached as a PDF. The letter as a Word document and the text message are in
   `umbraco/Data/Outbox/<request>`.
4. **See it fail properly.** Refuse an application without a reason. Nothing is sent, and the
   caseworker sees **Decision letter not sent** with what was missing.
5. **Change the letter.** In the backoffice, **Settings → Correspondence**, open the decision
   letter, change the text message, preview it with a sample and publish it. The next application
   is sent version 2; requests already sent keep the version they were given.

## How the two packages are joined

Neither package refers to the other. The join is about a hundred lines in the reference site,
the worked example for any site that has both:

- **One support system**, `heliograph`, with one capability, `send-correspondence`
  (`HeliographSupportSystem.cs`). A blueprint stage calls it with the correspondence's key as a
  fixed value and the recipient's email address as a field:

  ```yaml
  - type: support-system-call
    timing: onEnter
    params:
      supportSystemKey: heliograph
      capabilityKey: send-correspondence
      inputs:
        correspondence: decision-letter   # fixed: the correspondence to send
        email: applicantEmail             # a field: who it goes to
  ```

  The outcomes are `sent` and `failed`, and the blueprint routes on them.
- **The request is the data** (`RequestValues.cs`, `CorrespondenceData.cs`): its fields, its
  blueprint's calculated fields (the letter's `licenceApproved` is `decision = 'Approve'`), and
  two values the site adds, the date and a reference. Each is converted to the type the
  correspondence's dictionary gives it. A value the letter needs and the request lacks is left
  out, so Heliograph reports it and the stage resolves to `failed`.
- **Delivery is the site's** (`CorrespondenceDelivery.cs`): the email through Umbraco's mail
  settings, the letter and the text message to a folder. A real site sends the letter to a print
  house and the text through its SMS provider here.
- **It runs once the request is saved** (`HeliographSupportSystemClient.cs`). Wayfinder starts the
  call while it moves the request on, so the client sends in the background and reports the
  outcome to the engine the way an external system's call-back would.

Wayfinder's starter kit is for trying things on one machine and refuses to run anywhere else. A
real site wires Wayfinder.Umbraco itself; the join above is the same.
