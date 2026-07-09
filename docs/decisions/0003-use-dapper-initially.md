# ADR 0003: Use Dapper Initially For Legacy Database Access

## Status

Accepted.

Amended by ADR 0006 (2026-07-09): target client-library direction is EF Core while retaining stored procedures for hot paths. Follow-up work completed the migration of public-read repositories off Dapper; the Dapper package is no longer referenced in `QueenZone.Data`.

## Context

The legacy database uses many stored procedures and an older schema style. There is also old Telerik OpenAccess ORM code in the legacy solution, but that should not be brought forward as a dependency.

EF Core could be useful later, but modeling the whole legacy schema up front would slow down the first release.

## Decision

Use Dapper initially for legacy database reads.

Access patterns:

- Call stored procedures when they already match the needed page shape.
- Use direct SQL for simple read models when the stored procedure is too old, write-oriented, or awkward.
- Keep all legacy SQL access inside `QueenZone.Data`.

## Consequences

Benefits:

- Fast first vertical slice.
- Works naturally with stored procedures.
- Avoids mapping the whole legacy database.
- Keeps read models explicit.

Tradeoffs:

- Less compile-time modeling than EF Core.
- More manual SQL.
- Need disciplined tests around query mappings.

