# Modernization Plan

## Objective

Rebuild QueenZone as a modern, maintainable Azure-hosted site while preserving the valuable public content from the legacy ASP.NET Web Forms application.

The first release is intentionally read-only. Accounts, posting, uploads, private messages, newsletters, and administration are out of scope until the public archive is stable.

## Current Legacy Shape

The legacy site is:

- ASP.NET Web Forms on .NET Framework 4.5.
- Mostly VB.NET in `RO.QZ.Web`.
- SQL Server database `MAIN2_DB`.
- Heavy use of stored procedures.
- Old Telerik Web UI controls.
- Old Web Forms controls, master pages, handlers, and user controls.
- Mixed public content and community features in one database.

Legacy database inventory from `db-schema.sql`:

- 129 tables.
- 20 views.
- About 550 stored procedures.
- Full-text catalogs for blog, YouTube, and forum topic search.

## Target Shape

Recommended target:

- ASP.NET Core Razor Pages or MVC.
- C# for all new code.
- Azure SQL Database.
- Dapper initially for legacy schema access.
- Azure Blob Storage for pictures, downloads, and migrated media.
- Application Insights for telemetry.
- GitHub Actions for build and deploy.
- Markdown ADRs and migration docs from day one.

Alternative target to explore:

- Prerendered/static public site hosted on Azure Static Web Apps.
- Azure Functions for search, contact forms, and import utilities.
- App Service only for backend/admin needs if Functions become too limiting.

## Principles

- Preserve content first.
- Avoid copying old Web Forms architecture.
- Use simple direct SQL or stored procedure calls first.
- Prefer clean, stable, search-friendly canonical URLs over preserving legacy URL shapes.
- Treat personal/community data cautiously.
- Make public read-only pages safe before bringing back any write feature.
- Favor small vertical slices over broad rewrites.

## Phases

### Phase 0: Repository Setup

- Create clean repo.
- Add docs from this seed.
- Add `.editorconfig`, `.gitignore`, and basic CI.
- Add solution skeleton.

### Phase 1: News Slice

- Implement homepage latest news.
- Implement news archive.
- Implement news detail.
- Use stable, search-friendly canonical news URLs.
- Deploy to Azure preview.

### Phase 2: Core Content

- Articles.
- Biography.
- FAQ.
- Discography.
- Quotes and featured sites.

### Phase 3: Media

- Picture categories.
- Picture details.
- Blob Storage migration plan.
- Stable canonical media URLs.

Biography, album information, and the picture library are not optional nice-to-haves. They are core archive pillars for the relaunch and should be planned as first-class content sections.

### Phase 4: Archive Areas

- Forum read-only archive.
- Blog read-only archive.
- Search.

### Phase 5: Future Interactive Features

Possible later features:

- Admin-only content editing.
- Auth.
- Community profiles.
- Forum replacement or import.
- Newsletter rebuild.

These should be deliberate new features, not ports of old Web Forms pages.
