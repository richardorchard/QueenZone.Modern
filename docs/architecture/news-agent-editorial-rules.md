# News Agent Editorial Rules

Canonical editorial and source-safety rules for automated Queen-related news discovery. The machine-readable source registry lives in `src/QueenZone.NewsAgent/news-discovery-sources.json` and is seeded into the database with `--seed-sources`.

Related docs:

- `docs/architecture/news-agent.md` — pipeline, worker commands, admin review
- `docs/architecture/news-agent-scheduling.md` — scheduled runs (when merged)
- `docs/backlog/news-agent-mvp-handoff.md` — MVP handoff and GitHub tracking

## Publication boundary

- **Allowed:** automatic discovery, triage, and AI-assisted drafting into the admin review queue.
- **Not allowed:** automatic publication to visitor-facing `/news` or the homepage.
- Editors must explicitly promote, edit, and publish through `/admin/news`.

## Trust tiers

| Tier | Meaning | AI behaviour |
|------|---------|--------------|
| **Primary** | Official artist sites, attributable official announcements, linked label/venue pages | Lower relevance/confidence thresholds; drafts allowed when triage passes |
| **Secondary** | Reputable music press and broadcast entertainment desks | Higher thresholds; drafts only when confidence is strong |
| **Lead** *(not in registry yet)* | Fan sites, forums, aggregators, unattributed social | Review notes only by default; no confident draft unless corroborated by Primary/Secondary evidence |

Configured thresholds are in worker `appsettings.json` under `NewsTriage` and `NewsDraftGeneration`.

## Source registry

Seed file: `src/QueenZone.NewsAgent/news-discovery-sources.json`.

Each entry defines:

- `key` — stable identifier stored in the database
- `displayName` — editor-facing label
- `trustTier` — `Primary` or `Secondary`
- `sourceType` — `Rss`, `Sitemap`, or `AllowlistedPage`
- `pollIntervalMinutes` — minimum time between fetches (bypass with `--force`)
- `relevanceKeywords` — optional pre-filter before AI triage

### Primary sources (configured)

| Key | Site |
|-----|------|
| `queen-online` | https://www.queenonline.com/ |
| `brian-may` | https://brianmay.com/ |
| `roger-taylor` | https://www.rogertaylorofficial.com/ |
| `adam-lambert` | https://adamlambert.net/ |

Also in scope for future registry entries: official YouTube/community posts when attributable, tour promoters, venues, and record-label pages linked from official announcements.

### Secondary sources (configured)

| Key | Site |
|-----|------|
| `rolling-stone-music` | https://www.rollingstone.com/music/ |
| `billboard-music` | https://www.billboard.com/music/ |
| `nme-music` | https://www.nme.com/music |
| `louder` | https://www.loudersound.com/ |
| `ultimate-classic-rock` | https://ultimateclassicrock.com/ |
| `music-news` | https://www.music-news.com/ |
| `gold-radio-uk` | https://www.goldradiouk.com/ |
| `bbc-entertainment` | https://www.bbc.co.uk/news/entertainment_and_arts |

### Out of registry (manual review only)

Do not add without editorial discussion:

- Fan forums and fan blogs as automated Primary/Secondary sources
- Aggregators that republish without clear attribution
- Unsourced social posts
- Any login-gated, deleted, moderated, or private content

## Relevance scope

### In scope

- Queen as a band; Queen + Adam Lambert; official tours, releases, and exhibitions
- Freddie Mercury estate and major public anniversaries, auctions, documentaries, books
- Brian May, Roger Taylor, John Deacon (official or major public updates)
- Adam Lambert when tied to Queen or when editors want major solo news
- Charity events, awards, archive discoveries, reissues, and box sets

### Out of scope by default

- Private or non-public information
- Rumours without credible sourcing
- Hidden, deleted, moderated, or credential-related material
- Generic celebrity gossip with no QueenZone editorial value
- Passing mentions of Queen in unrelated stories

AI triage should **reject** out-of-scope items. Editors can always override by creating manual news in `/admin/news`.

## Drafting rules

- Drafts must be **original QueenZone prose** grounded in fetched excerpts — never copy full source articles or long passages.
- Every draft must retain **source links and attribution** (`AttributionText`, `SourceNotes`).
- Uncertainty must appear in `ConfidenceNotes` when the model is not sure.
- Lower-confidence leads should prefer internal review notes over confident public-facing draft text unless corroborated.

## Editor workflow expectations

On `/admin/news-discovery` editors should:

1. Inspect source URL, evidence, and AI rationale.
2. **Mark not relevant** or **Ignore duplicate** when appropriate.
3. **Generate / regenerate draft** (admin UI or worker) when triage passed but prose is missing or stale.
4. **Edit draft** fields before promotion.
5. **Promote to admin news** — creates an unpublished article; does not publish.
6. **Publish** only from `/admin/news` after deliberate review.

Rejected and ignored-duplicate candidates remain in the database to suppress repeat proposals.

## Safety rules (non-negotiable)

- Do not commit API keys or connection strings.
- Do not scrape logged-in, paywalled, or robots-disallowed content.
- Do not expose candidates, drafts, or low-confidence rumours on public pages.
- Preserve provenance on every candidate, draft, and promoted article.
- Default CI tests use fakes; report when real OpenRouter or live feed smoke tests were run or skipped.

## Changing these rules

1. Update this document and `news-discovery-sources.json` together when adding/removing sources.
2. Re-seed with `discover-news --seed-sources` (or scheduled run) after registry changes.
3. Adjust `NewsTriage` / `NewsDraftGeneration` thresholds in worker `appsettings.json` when editorial policy tightens or relaxes.
