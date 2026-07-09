# Data Migration Plan

## Strategy

For the archive-first launch, do not transform the whole database up front.

Use the restored legacy SQL Server database as a read source, then introduce modern projection tables or import jobs only when the shape of each content area is understood.

The legacy schema remains a stable historical source for most public archive reads. News combines archived legacy records from `NEWS_T` with newly approved articles stored through modern admin/discovery tables. Forum data has already been projected into `ModernForum*` tables for public reads because the legacy topic/reply model was too large and awkward for modern archive browsing. Other content areas should stay on legacy reads unless performance or safety problems appear.

## Data Access Stages

### Stage 1: Read From Legacy Schema

Use Dapper to call either:

- Existing stored procedures, when they already return the page shape needed.
- Direct SQL queries, when the stored procedure is too page-specific or outdated.

This keeps the first release quick and reduces assumptions.

### Stage 2: Create Modern Read Models When Needed

Forum already uses modern read models for public archive pages. For other high-value content, create modern read models only when legacy reads become a performance, privacy, or maintainability problem:

- `ContentPage`
- `NewsItem`
- `Article`
- `Album`
- `Song`
- `Picture`
- `CanonicalRoute`

These can be built by an import job and stored in new tables, or materialized into static JSON for selected pages.

News should move toward a combined read model early so new approved articles can be added without mutating `NEWS_T`.

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
- Canonical route metadata for stable public URLs.
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

Storage policy:

- Leave `NEWS_T` as the historical archive source.
- Do not insert new live articles into `NEWS_T`.
- Create separate modern SQL tables for live news articles, draft/review workflow, source metadata, and review events.
- Build the public news query layer so archived `NEWS_T` rows and new article rows appear as one seamless news collection.
- Keep the storage origin available internally for audit, dedupe, and troubleshooting.

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

Visitor-facing behavior:

- The homepage and `/news` list should interleave legacy and new articles by publication date.
- Detail pages should use one canonical URL pattern regardless of whether an item came from `NEWS_T` or modern live-news tables.
- The UI should not label articles as "legacy" or "new" unless there is an editorial reason to do so.

URL policy:

- Do not preserve old Web Forms URL shapes by default.
- Use stable, search-friendly canonical URLs for archived and newly published pages.
- Keep legacy URL notes only when they help understand content identity or relationships.

## Validation Checklist Per Content Area

- Count published legacy records.
- Count records rendered by new app.
- Spot check 10 oldest, 10 newest, and 10 random records.
- Check HTML rendering.
- Check links and media paths.
- Check encoding and special characters.
- Check canonical URL generation.
- Check no private fields are exposed.
