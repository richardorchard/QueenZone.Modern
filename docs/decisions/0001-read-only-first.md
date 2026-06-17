# ADR 0001: Archive-First Public Site With Live News

## Status

Accepted.

## Context

The legacy QueenZone application combines public content, community features, administration, uploads, email, private messages, forums, and tracker/download functionality in one old ASP.NET Web Forms codebase.

Recreating all behavior at once would delay public value and increase security/privacy risk.

## Decision

The first modern release will be archive-first and focused on public content:

- News.
- Articles.
- Biography.
- FAQ.
- Discography.
- Pictures after asset review.
- Optional read-only archives later.

The public archive will be read-only for visitors. News is the first live editorial slice: approved editors may add new news articles through a deliberate workflow once the modern news model exists. Legacy `NEWS_T` remains the historical archive source; newly approved live articles should be stored in separate modern tables and presented seamlessly with archived news.

The first release will not include:

- Login.
- Posting.
- Uploads.
- Private messages.
- Newsletter sending.
- Broad admin editing outside the news workflow.
- Torrent/tracker/download features.

## Consequences

Benefits:

- Faster public relaunch.
- Lower security risk.
- Smaller migration surface.
- Easier Azure deployment.
- Allows content quality review before interactive features return.
- Allows QueenZone to become current again through editor-approved news without reopening public posting.

Tradeoffs:

- Community functionality is delayed.
- Old Web Forms URL shapes are not preserved by default because the site has been offline for years.
- Admin workflows need deliberate design, starting with the constrained news editorial workflow.
