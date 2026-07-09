# ADR 0006: Hybrid EF Core For Admin Writes

## Status

Accepted.

Amended 2026-07-09: documented the full Dapper vs EF repository matrix, contributor rules, and a target direction to standardize on EF Core as the single data-access library while keeping stored procedures for hot read paths.

Amended 2026-07-09: migrated biography public reads to EF Core (`EfBiographyRepository`) while retaining the same legacy stored procedures.

## Context

ADR 0003 chose Dapper for initial legacy database access. Issue #5 added admin write workflows for `NEWS_T` and `NewsAuditLog` using hand-written SQL.

Admin writes benefit from typed entities, change tracking, and migrations. Public news reads still rely on legacy deduplication (`ROW_NUMBER()` over `NEWS_ID`) and remain a better fit for explicit SQL.

As more content areas landed, the hybrid split grew beyond news. Without a written matrix, new work can accidentally introduce a third SQL style (ad-hoc SQL in page models or tools) or pick Dapper/EF inconsistently.

## Decision

Use a hybrid data-access model in `QueenZone.Data` today, with a clear path toward one library:

### Current split

- **Dapper** for many public read repositories (legacy tables/procs and modern forum procs).
- **EF Core** for admin/editorial writes, modern workflow tables, and some modern reads.
- Map legacy `NEWS_T` in EF as an existing table excluded from schema migrations where admin news needs it.
- Manage modern tables (`NewsAuditLog`, discovery, members, queen history, and similar) through EF migrations plus any required SQL bootstrap scripts.
- Keep **all** SQL Server access inside `QueenZone.Data`. Page models and tools must not open their own connections or invent a third SQL style.

`QueenZoneDbContext` is registered scoped. EF-backed repositories (including migrated public reads such as biography) are scoped. Remaining public Dapper repositories stay singleton until migrated. In-memory repositories are still used when no SQL connection string is configured.

### Access matrix (SQL-backed registrations)

| Content / concern | Interface | SQL implementation | Access style | Notes |
| --- | --- | --- | --- | --- |
| Public news archive | `INewsRepository` | `LegacyNewsRepository` | Dapper + SQL | Legacy `NEWS_T` latest-row projections |
| Articles | `IArticlesRepository` | `LegacyArticlesRepository` | Dapper | Legacy reads |
| Biography | `IBiographyRepository` | `EfBiographyRepository` | EF Core + stored procedures (`SqlQuery` / `SqlQueryRaw`) | Same legacy procs (`Q_BIO_LIST_SP`, `Q_BIO_DISPLAY_SP`); first public-read migration off Dapper |
| Photography | `IPhotoRepository` | `LegacyPhotoRepository` | Dapper + stored procedures | Legacy procs |
| Discography | `IDiscographyRepository` | `LegacyDiscographyRepository` | Dapper + stored procedures | Legacy procs |
| Fan performances | `IFanPerformanceRepository` | `LegacyFanPerformanceRepository` | Dapper + stored procedures | Legacy procs |
| Legacy member lookup | `ILegacyMemberLookupRepository` | `LegacyMemberLookupRepository` | Dapper | Legacy reads |
| Forum (default) | `IForumRepository` | `ModernForumRepository` | Dapper + stored procedures | Modern `ModernForum*` tables; procs in `docs/sql/006-modern-forum-read-path.sql` |
| Forum (rollback) | `IForumRepository` | `LegacyForumRepository` | Dapper + stored procedures | Opt-out via `ForumData:UseModernForumReads = false` |
| Admin news writes | `IAdminNewsRepository` | `EfAdminNewsRepository` | EF Core (+ `FromSqlRaw` for legacy news projections) | Writes/migrations; hybrid SQL for `NEWS_T` reads used by admin |
| News audit | `INewsAuditRepository` | `EfNewsAuditRepository` | EF Core | Modern audit table |
| Member accounts | `IMemberAccountRepository` | `EfMemberAccountRepository` | EF Core | Modern tables |
| News discovery / agent drafts | `INewsDiscoveryRepository` | `EfNewsDiscoveryRepository` | EF Core | Modern workflow tables |
| News agent run leases | `INewsAgentRunLeaseService` | `EfNewsAgentRunLeaseService` | EF Core (+ SQL for lease upsert) | Modern lease table |
| Queen history / on-this-day | `IQueenHistoryRepository` | `EfQueenHistoryRepository` | EF Core | Modern table |

When `ConnectionStrings:QueenZoneLegacy` is empty, the matching `InMemory*` implementations are used instead (local/Testing).

### Contributor rules

1. **New write paths default to EF Core** against deliberately designed modern tables (or carefully mapped legacy tables when that is the accepted write target).
2. **Complex legacy or projected read shapes may keep explicit SQL/stored procedures** until a modern table or EF mapping is justified.
3. **Do not add a third ad-hoc SQL style** in Razor page models, minimal endpoints, or tools. New SQL belongs in `QueenZone.Data` repositories.
4. Prefer extending an existing repository over opening a one-off `SqlConnection` elsewhere.

### Target direction (standardize on EF Core)

It is desirable to standardize on **EF Core as the single data-access library** for both old and new tables, without giving up stored procedures where they already win on shape or performance.

Important distinction:

- **Library choice** (Dapper vs EF) is mostly about C# mapping, DI, and migrations.
- **Query performance** for an existing stored procedure is dominated by SQL Server plans and indexes. Calling the same proc through EF (`FromSqlRaw` / `Database.SqlQuery` / `ExecuteSql`) keeps that server-side work; it does not require rewriting procs as LINQ.

Target end state:

- One primary library: EF Core in `QueenZone.Data`.
- Keep valuable stored procedures (especially `ModernForum_*` and proven legacy procs) and invoke them from EF-backed repositories.
- Use EF entities/change tracking for writes and for simple modern-table reads.
- Migrate existing `Legacy*` / Dapper repositories incrementally when those areas are touched, not as a big-bang rewrite.
- Do not map the entire legacy schema into EF entities solely to call procs; keyless entities or SQL queries are enough for read models.

This target does **not** require abandoning ADR 0004's "legacy as import source" policy. Table shape (legacy vs modern) remains independent of the client library.

## Consequences

Benefits:

- Admin write code uses EF `ExecuteUpdate`, `ExecuteDelete`, and `SaveChanges` instead of manual SQL strings.
- Modern schema is versioned through EF migrations.
- Public read queries can stay explicit (SQL/procs) where that is the right tool.
- Contributors have a single matrix and a clear default for new work.
- A future EF-only client stack can still preserve proc performance.

Tradeoffs:

- Two data-access styles remain in `QueenZone.Data` until repositories are migrated.
- `NEWS_T` is mapped with `NEWS_ID` as the EF key; duplicate legacy rows are read through a latest-row SQL projection.
- Deployments must apply both bootstrap SQL scripts (where required) and `dotnet ef database update` for EF-managed tables.
- Moving Dapper repositories to EF is follow-up engineering work, not part of the docs-only matrix pass.
