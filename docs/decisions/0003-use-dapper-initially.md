# ADR 0003: Use Dapper Initially For Legacy Database Access

## Status

Accepted.

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

