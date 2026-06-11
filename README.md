# QueenZone Modern

This repository contains the new QueenZone modernization project.

The goal is to restart QueenZone as a clean, modern, read-only public site first, using the legacy database and application as reference material rather than carrying the whole Web Forms codebase forward.

## Repository Shape

```text
/
  QueenZone.sln
  src/
    QueenZone.Web/
    QueenZone.Data/
    QueenZone.Import/
  tests/
    QueenZone.Data.Tests/
    QueenZone.Web.Tests/
  docs/
    architecture/
    legacy/
    decisions/
    backlog/
```

## Local Development

The app targets .NET 10.

```powershell
dotnet restore QueenZone.sln
dotnet build QueenZone.sln
dotnet test QueenZone.sln
dotnet run --project src/QueenZone.Web/QueenZone.Web.csproj
```

Local secrets belong in `src/QueenZone.Web/appsettings.Local.json`, which is ignored by git. You can also set `ConnectionStrings__QueenZoneLegacy` in your shell or a local `.env` file for tooling that loads dotenv values. If no `ConnectionStrings:QueenZoneLegacy` value is present, the site uses sample news data so the first slice can still run locally.

## First Milestone

Build the first vertical slice around news:

1. Connect to the restored legacy SQL Server database.
2. Read published rows from `NEWS_T`, either directly or through `Q_NEWS_*` stored procedures.
3. Render latest news on the homepage.
4. Render `/news` archive.
5. Render `/news/{id}/{slug}` detail pages.
6. Add redirects for old news URLs.
7. Deploy a preview to Azure App Service.

## Legacy Reference Policy

Keep the old repository available locally as an archaeological reference, but do not copy it wholesale into the new repo.

Useful reference files belong in `docs/legacy`:

- `db-schema.sql`
- table map
- stored procedure index
- old URL map
- content inventory

The old source is only copied or ported when a specific page or behavior needs to be understood.
