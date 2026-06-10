# QueenZone Modern - Repository Seed

This folder is a documentation seed for the new QueenZone modernization repository.

The goal is to restart QueenZone as a clean, modern, read-only public site first, using the legacy database and application as reference material rather than carrying the whole Web Forms codebase forward.

## Recommended Repository Shape

```text
/
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

