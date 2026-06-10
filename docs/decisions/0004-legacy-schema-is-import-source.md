# ADR 0004: Treat Legacy Schema As Import Source

## Status

Accepted.

## Context

The legacy database is valuable because it contains the historical QueenZone content. It is also old, highly procedural, and mixes public content with user accounts, private data, community features, tracking data, and administrative concerns.

Some areas, especially forums, are unlikely to be efficient or pleasant to query directly for a modern public archive.

## Decision

The legacy schema is an import source, not the final schema.

The modern application may read directly from legacy tables and stored procedures during early migration, but it can introduce new modern read-model tables when that improves safety, performance, clarity, or maintainability.

Forum archive work should strongly prefer modern projected tables over direct long-term reads from `Q_FORUM_TOPIC_T`.

## Consequences

Benefits:

- We can launch quickly while still allowing a better long-term shape.
- Forum archive pages can be optimized for modern browsing and search.
- Private/sensitive fields can be excluded at import time.
- Old IDs and URLs can still be preserved.

Tradeoffs:

- Requires import jobs and repeatable migration scripts.
- Data correctness needs validation reports.
- There may be a period where both legacy and modern tables exist.

