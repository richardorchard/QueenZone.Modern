# ADR 0006: Hybrid EF Core For Admin Writes

## Status

Accepted.

## Context

ADR 0003 chose Dapper for initial legacy database access. Issue #5 added admin write workflows for `NEWS_T` and `NewsAuditLog` using hand-written SQL.

Admin writes benefit from typed entities, change tracking, and migrations. Public news reads still rely on legacy deduplication (`ROW_NUMBER()` over `NEWS_ID`) and remain a better fit for explicit SQL.

## Decision

Use a hybrid data-access model in `QueenZone.Data`:

- **Dapper** for public read-only `INewsRepository` (`LegacyNewsRepository`).
- **EF Core** for admin writes and audit logging (`EfAdminNewsRepository`, `EfNewsAuditRepository`).
- Map legacy `NEWS_T` in EF as an existing table excluded from schema migrations.
- Manage `NewsAuditLog` and admin extension columns through EF migrations plus the existing SQL bootstrap script.

`QueenZoneDbContext` is registered scoped. Public news access remains singleton Dapper. In-memory repositories are still used when no SQL connection string is configured.

## Consequences

Benefits:

- Admin write code uses EF `ExecuteUpdate`, `ExecuteDelete`, and `SaveChanges` instead of manual SQL strings.
- `NewsAuditLog` schema is versioned through EF migrations.
- Public read queries stay explicit and unchanged.

Tradeoffs:

- Two data-access styles remain in `QueenZone.Data`.
- `NEWS_T` is mapped with `NEWS_ID` as the EF key; duplicate legacy rows are read through a latest-row SQL projection.
- Deployments must apply both `docs/sql/001-news-admin-columns.sql` (or the equivalent EF migration) and `dotnet ef database update` for `NewsAuditLog`.