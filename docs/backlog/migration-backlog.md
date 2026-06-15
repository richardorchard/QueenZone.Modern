# Migration Backlog

## Epic: Repository Foundation

### Create solution skeleton

Acceptance criteria:

- `QueenZone.sln` exists.
- `src/QueenZone.Web` exists.
- `src/QueenZone.Data` exists.
- `tests` folder exists.
- App runs locally.

### Add CI

Acceptance criteria:

- GitHub Actions builds the solution.
- Tests run.
- Failed build blocks deployment.

### Add basic observability

Acceptance criteria:

- Application Insights configured in Azure preview.
- App logs startup and request failures.
- Health endpoint exists.

## Epic: Hosting Exploration

### Prototype prerendered Static Web Apps deployment

Acceptance criteria:

- Static HTML generated for homepage and at least one content type.
- Sitemap generated at build time.
- Preview deploy works in Azure Static Web Apps.
- A short decision note compares Static Web Apps with App Service based on the prototype.

### Prototype Function-backed search endpoint

Acceptance criteria:

- A Function accepts a simple search or lookup request.
- Function returns crawl-safe structured results or metadata.
- Static Web Apps can route or proxy to the Function.
- Timeout and API constraints are documented.

## Epic: SEO And Monetisation

### Add SEO foundation

Acceptance criteria:

- Unique title and meta description per page.
- Canonical URL support.
- XML sitemap.
- Robots.txt.
- Open Graph metadata.
- Structured data plan for articles/albums/images.

### Explore monetisation options

Acceptance criteria:

- Display ads, affiliate links, and donation/support options compared.
- Page-speed impact documented.
- Initial no-ads/ad-light policy decided before production launch.

## Epic: News Vertical Slice

### Implement legacy DB connection

Acceptance criteria:

- Connection string loaded from configuration.
- No secrets committed.
- Local dev can connect to restored DB.
- Azure preview can connect to Azure SQL.

### Render latest news on homepage

Acceptance criteria:

- Homepage shows latest published news.
- Only `DISPLAY = 1` records appear.
- Empty state is handled.

### Render news archive

Acceptance criteria:

- `/news` lists published news.
- Pagination works.
- Dates render consistently.

### Render news detail

Acceptance criteria:

- `/news/{id}/{slug}` renders title, date, excerpt/body, and source if approved.
- Missing IDs return 404.
- Hidden items do not render publicly.

### Confirm canonical news URLs

Acceptance criteria:

- `/news` is the canonical archive URL.
- `/news/{id}/{slug}` is the canonical detail URL.
- Wrong slugs redirect to the canonical slug.
- Canonical route behavior is covered by tests.

## Epic: Articles And Biography

### Render articles archive and detail

Acceptance criteria:

- `/articles` lists published articles.
- Category pages work.
- Detail pages render old rich text safely.

### Render biography pages

Acceptance criteria:

- `/biography` lists biography sections.
- `/biography/{id}/{slug}` renders detail.
- Ordering matches legacy intent.

### Treat biography as core archive content

Acceptance criteria:

- Biography pages are included in sitemap.
- Biography pages use stable canonical URLs.
- Metadata is tuned for search.
- Rendering preserves old content while improving readability.

## Epic: Discography

### Render album list and detail

Acceptance criteria:

- `/discography` lists active albums.
- Album detail includes notes, artwork if available, and songs.
- Album pages use stable canonical URLs.

### Treat album information as core archive content

Acceptance criteria:

- Album pages include release date, notes, artwork, and track listing where available.
- Song pages exist if content policy allows.
- Lyrics policy is applied consistently.
- Album pages include structured metadata where appropriate.

### Decide lyric policy

Acceptance criteria:

- A written decision exists for whether to publish `SONG_LYRICS`.
- Implementation follows that decision.

## Epic: Pictures

### Audit picture paths

Acceptance criteria:

- Count of `PIC_FILES_T` records.
- Count of referenced files found.
- Count of missing files.
- Path normalization rules documented.

### Treat picture library as core archive content

Acceptance criteria:

- Picture category pages are planned for first public archive release.
- Picture detail pages have canonical URLs.
- Image sitemap plan exists.
- Blob Storage source and backup source are reconciled.

### Migrate public pictures to Blob Storage

Acceptance criteria:

- Public images copied to Blob Storage.
- Thumbnails are present or regenerated.
- New app uses Blob URLs.

## Epic: Archive

### Forum archive feasibility review

Acceptance criteria:

- Public/private fields identified.
- Deleted/hidden/moderated content rules documented.
- Sample forum topic renders read-only.

### Design modern forum archive schema

Acceptance criteria:

- Proposed tables documented.
- Required indexes documented.
- Legacy `Q_FORUM_TOPIC_T` mapping documented.
- Private fields excluded by design.
- Canonical URL strategy documented.

### Build forum import proof of concept

Acceptance criteria:

- One forum category imports into modern tables.
- Thread and post counts match legacy source for that category.
- Import report lists skipped or unsafe records.
- Read-only pages render from modern tables, not directly from legacy forum tables.

### Blog archive feasibility review

Acceptance criteria:

- Blog ownership/profile exposure reviewed.
- Comments policy documented.
- Sample blog post renders read-only.
