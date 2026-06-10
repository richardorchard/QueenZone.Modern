# Data Migration Plan

## Strategy

For the read-only launch, do not transform the whole database up front.

Use the restored legacy SQL Server database as a read source, then introduce modern projection tables or import jobs only when the shape of each content area is understood.

The legacy schema is not considered permanent. It is acceptable, and likely necessary, to introduce modern tables for high-value areas once the read-only import proves what content and relationships matter. Forum data is the clearest candidate for redesign because the legacy topic/reply model is old, large, and not optimized for modern archive browsing.

## Data Access Stages

### Stage 1: Read From Legacy Schema

Use Dapper to call either:

- Existing stored procedures, when they already return the page shape needed.
- Direct SQL queries, when the stored procedure is too page-specific or outdated.

This keeps the first release quick and reduces assumptions.

### Stage 2: Create Modern Read Models

For high-value content, create modern read models:

- `ContentPage`
- `NewsItem`
- `Article`
- `Album`
- `Song`
- `Picture`
- `LegacyRedirect`

These can be built by an import job and stored in new tables, or materialized into static JSON for selected pages.

### Stage 3: Asset Migration

Move media out of legacy filesystem assumptions:

- Original pictures.
- Thumbnails.
- Album art.
- Avatars if ever exposed.
- Downloads only after legal review.

Target storage:

- Azure Blob Storage.
- CDN or Azure Front Door later if traffic warrants.

### Stage 4: Modernized Domain Tables

After a content area is stable, create modern tables instead of forcing the app to keep querying inefficient legacy shapes forever.

Good candidates:

- Forum archive: normalized thread/post/read-model tables with indexes for archive browsing.
- Search documents: flattened searchable content rows.
- Redirects: legacy URL to canonical route table.
- Media assets: canonical media records pointing at Blob Storage.
- Content pages: sanitized, rendered, and metadata-enriched public content.

The legacy database should remain reproducible as an import source. Modern tables should be created through migrations and documented in ADRs.

## Sensitive Data Rules

Never publish by default:

- Emails.
- Passwords or password hints.
- IP addresses.
- Private messages.
- Private profile fields.
- Admin notes.
- Unvalidated submissions.
- Hidden/deleted/forum moderation content.

User-generated public text still needs review before being shown in bulk.

## First Data Slice: News

Legacy table:

- `NEWS_T`

Relevant columns:

- `NEWS_ID`
- `TITLE`
- `EXCERPT`
- `ARTICLE`
- `DATE`
- `USER_ID`
- `DISPLAY`
- `TYPE`
- `QUEEN_ONLINE`
- `SOURCE_URL`

Relevant procedures:

- `Q_NEWS_FRONT_PAGE_DISPLAY_SP`
- `Q_NEWS_ITEM_DISPLAY_SP`
- `Q_NEWS_VIEW_PAGE_SP`
- `Q_NEWS_LIST_ALL_SP`

Initial route mapping:

- `/` latest news block.
- `/news`
- `/news/{id}/{slug}`

Old URL redirects:

- `/news.aspx`
- `/process/news_view.aspx?news_id={id}`
- Any rewrite-backed news paths from old `web.config`.

## Validation Checklist Per Content Area

- Count published legacy records.
- Count records rendered by new app.
- Spot check 10 oldest, 10 newest, and 10 random records.
- Check HTML rendering.
- Check links and media paths.
- Check encoding and special characters.
- Check old URL redirect.
- Check no private fields are exposed.
