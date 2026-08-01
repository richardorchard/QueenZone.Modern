# SQL scripts

Authoritative SQL for stored procedures, FTS DDL, and other hand-written database objects that are **not** fully expressed as EF LINQ.

## Contributor convention

1. **Large procedure bodies and FTS/bootstrap DDL live here first** (`docs/sql/*.sql`) so they are easy to review and re-run in SSMS.
2. **EF migrations apply the same SQL** (embedded in the migration for deploy reliability). When you change a proc:
   - Update the `.sql` file in this folder.
   - Update the matching migration (or add a new idempotent `CREATE OR ALTER` migration) so production still receives the change.
3. Prefer `CREATE OR ALTER PROCEDURE` and idempotent `IF NOT EXISTS` DDL.
4. FTS catalog/index creation must use `suppressTransaction: true` in migrations (SQL Server limitation).

## Inventory (selected)

| File | Purpose |
| --- | --- |
| `006-modern-forum-read-path.sql` | Modern forum read procs + indexes |
| `007-forum-search.sql` / related | Forum FTS |
| `008-news-full-text-search.sql` | `dbo.NEWS_T_SearchPublished` proc body (migration `20260729000000_AddNewsFullTextSearch`) |
| `009-photo-dimension-inventory.sql` | Read-only coverage of `PIC_WIDTH`/`PIC_HEIGHT` for public photos (issue #435) |

Do not put connection strings or secrets in these files.

### Photo dimension inventory (#435)

```powershell
# Formatted tool report (preferred)
dotnet run --project src/QueenZone.Tools -- photo-dim-inventory --connection-string $env:ConnectionStrings__QueenZoneLegacy

# Or run the SQL script in SSMS / sqlcmd against the legacy database
# docs/sql/009-photo-dimension-inventory.sql
```

Post results as a comment on GitHub issue #435 for filter authors (#437).
