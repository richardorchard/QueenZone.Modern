# ADR 0001: Read-Only Public Site First

## Status

Accepted.

## Context

The legacy QueenZone application combines public content, community features, administration, uploads, email, private messages, forums, and tracker/download functionality in one old ASP.NET Web Forms codebase.

Recreating all behavior at once would delay public value and increase security/privacy risk.

## Decision

The first modern release will be read-only and focused on public content:

- News.
- Articles.
- Biography.
- FAQ.
- Discography.
- Pictures after asset review.
- Optional read-only archives later.

The first release will not include:

- Login.
- Posting.
- Uploads.
- Private messages.
- Newsletter sending.
- Admin editing.
- Torrent/tracker/download features.

## Consequences

Benefits:

- Faster public relaunch.
- Lower security risk.
- Smaller migration surface.
- Easier Azure deployment.
- Allows content quality review before interactive features return.

Tradeoffs:

- Community functionality is delayed.
- Some legacy URLs may initially redirect to archive placeholders.
- Admin workflows need a separate future design.

