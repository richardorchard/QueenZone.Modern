# News Agent MVP Handoff

This handoff gives a future agent the full context for building the first automated Queen-related news discovery workflow.

**Operational guide:** `docs/architecture/news-agent.md` (worker commands, local secrets, smoke test, admin review queue).

## Implementation status (March 2026)

| Area | Status |
|------|--------|
| Source fetchers + worker (#101) | Done — `QueenZone.NewsAgent`, `QueenZone.NewsAgent.Worker`, `discover-news` |
| OpenRouter + budgets (#102) | Done |
| AI triage (#103) | Done |
| Draft generation (#104) | Done |
| Admin review queue (#105) | Done — `/admin/news-discovery` + in-admin regenerate draft |
| Promote to live news (#106) | Done — provenance on admin news edit/preview, richer audit, bidirectional links |
| Scheduled hosting (#107) | Done — DB run lease, `--scheduled`, `scripts/Run-NewsAgentDiscovery.ps1`, `docs/architecture/news-agent-scheduling.md` |
| Tests / observability (#108) | Done — run telemetry, failure-mode tests, documented test matrix |
| Source registry doc (#99) | Done — `news-discovery-sources.json` + `docs/architecture/news-agent-editorial-rules.md` |

Safe boundary in production today: discovery and drafting run via the worker; editors review at `/admin/news-discovery`; public pages only change after explicit publish in `/admin/news`.

## Objective

Build an automatic discovery and drafting pipeline for QueenZone news while keeping publication deliberately human-approved.

The agent/application should discover public Queen-related news, evaluate whether it is relevant, create editor-reviewable draft candidates, and place them in an admin queue. It must not publish directly to visitor-facing pages.

This supports the existing QueenZone Modern direction:

- Archive-first public site.
- Visitor-facing archive pages remain read-only.
- News is the first live editorial slice.
- Newly approved news articles can be created through protected editorial workflows.
- AI can assist discovery and drafting, but editors approve publication.

## GitHub Tracking

Use milestone `News Agent MVP`.

Use shared label `epic:news-agent-mvp`.

Area labels:

- `area:news-sources`
- `area:ai-agent`
- `area:editorial-workflow`
- `area:hosting-automation`

Current implementation issues:

- #99 Define trusted source registry and editorial rules for automated Queen news discovery. *(done)*
- #100 Add database model for news discovery sources, candidates, evidence, and AI draft metadata. *(done)*
- #101 Implement source fetchers for RSS, sitemap, and allowlisted web pages. *(done)*
- #102 Add OpenRouter client configuration with low-cost model defaults and budget controls. *(done)*
- #103 Implement AI triage for relevance, deduplication, and confidence scoring of discovered news. *(done)*
- #104 Generate editor-reviewable news drafts with citations from approved candidates. *(done)*
- #105 Build admin review queue for discovered candidates and generated drafts. *(done)*
- #106 Promote approved AI-assisted drafts into the live news article workflow. *(done)*
- #107 Add scheduled news discovery worker with local-first and Azure hosting options. *(done — lease, scheduling doc, Task Scheduler script)*
- #108 Add tests, observability, and safety checks for the automated news agent MVP. *(done)*

## Product Shape

The intended flow is:

1. A scheduled worker fetches configured public sources.
2. Source items are normalized and deduplicated into candidate records.
3. A low-cost AI triage step classifies relevance and confidence.
4. High-confidence candidates can receive generated draft fields.
5. Editors review candidates and drafts in an authenticated admin Razor Pages UI.
6. Editors can reject, ignore duplicates, edit drafts, promote drafts, schedule, or publish.
7. Public pages render only approved/published news according to existing publication rules.

The safe boundary is important: automatic discovery and drafting are allowed; automatic public publishing is not part of this MVP.

## Recommended Sources

Primary / highest confidence:

- Official Queen site: https://www.queenonline.com/
- Brian May official site: https://brianmay.com/
- Roger Taylor official site: https://www.rogertaylorofficial.com/
- Adam Lambert official site: https://adamlambert.net/
- Official public artist social/news surfaces where attributable.
- Tour promoters, venues, record labels, or release pages linked from official announcements.

Secondary / reputable music press:

- Rolling Stone Music: https://www.rollingstone.com/music/
- Billboard Music: https://www.billboard.com/music/
- NME Music: https://www.nme.com/music
- Louder / Classic Rock: https://www.loudersound.com/
- Ultimate Classic Rock: https://ultimateclassicrock.com/
- Music-News: https://www.music-news.com/
- Gold Radio: https://www.goldradiouk.com/
- BBC Entertainment & Arts: https://www.bbc.co.uk/news/entertainment_and_arts

Lower-confidence leads:

- Fan sites.
- Forums.
- Aggregators.
- Unsourced social posts.

Lower-confidence leads should create review notes, not confident drafts, unless confirmed by a primary or reputable secondary source.

## Relevance Scope

Treat these as in scope:

- Queen as a band.
- Freddie Mercury estate, official releases, exhibitions, auctions, and major anniversaries.
- Brian May.
- Roger Taylor.
- John Deacon, especially official or major public updates.
- Adam Lambert where related to Queen, Queen + Adam Lambert, or major solo news that QueenZone editors want to cover.
- Official releases, reissues, box sets, documentaries, books, concerts, tours, exhibitions, charity events, major awards, and archive discoveries.

Treat these as out of scope by default:

- Private or non-public information.
- Rumors without credible sourcing.
- Hidden, deleted, moderated, credential-related, or leaked material.
- Generic celebrity gossip with no QueenZone editorial value.
- Articles that only mention Queen in passing.

## AI Model Guidance

OpenRouter is a good fit because it provides a single OpenAI-compatible API for multiple models.

Use the app to fetch sources directly. Do not depend on the model as the primary web browser. This keeps cost, hallucination risk, and source-control risk lower.

Suggested initial model defaults:

- Triage, relevance, dedupe: `openai/gpt-4.1-nano`.
- Draft generation: `openai/gpt-4.1-mini`.
- Optional fallback/second opinion: `deepseek/deepseek-chat-v3-0324` or a current low-cost Gemini Flash model available through OpenRouter.

Required controls:

- Missing `OPENROUTER_API_KEY` disables AI processing cleanly.
- Support dry-run mode.
- Configurable per-run candidate limit.
- Configurable daily or per-run budget.
- Log model id, prompt version, token usage, and estimated cost where available.
- Use structured output for triage and draft generation.

## Data Model Intent

Keep generated/discovered material separate from visitor-facing approved news.

Expected concepts:

- `NewsSource`: configured source, source type, trust tier, cadence, enabled state.
- `NewsCandidate`: normalized discovered story candidate.
- `NewsCandidateEvidence`: source URL, canonical URL, title, date, source name, fetched excerpt/hash, provenance.
- `NewsAiRun` or equivalent: model id, prompt version, status, token/cost metadata, structured result.
- Draft metadata: generated title, summary, body, slug suggestion, source notes, confidence notes.

Useful candidate statuses:

- `Discovered`
- `NeedsReview`
- `Drafted`
- `Rejected`
- `IgnoredDuplicate`
- `PromotedToArticle`

Do not make candidates public just because they exist.

## Admin Workflow

Admin pages live under `src/QueenZone.Web/Pages` as Razor Pages.

**Implemented (#105):** `Pages/Admin/NewsDiscovery/`

- `/admin/news-discovery` — filterable candidate list
- `/admin/news-discovery/{id}` — review detail (source URLs, evidence, AI rationale, draft)
- `/admin/news-discovery/{id}/edit-draft` — edit generated draft fields

Editor actions: reject, ignore duplicate, edit draft, promote to admin news (creates unpublished article in `/admin/news`).

**Existing manual news workflow:** `Pages/Admin/News/` — publish remains explicit here.

Expected editor actions (full MVP):

- Filter by status, source, trust tier, confidence, related entity, and date.
- Inspect source URLs and source notes.
- See AI rationale and confidence.
- Mark not relevant.
- Ignore duplicate.
- Generate or regenerate draft.
- Edit draft fields.
- Promote draft to the modern news article workflow.
- Schedule or publish only through an explicit editor action.

All state-changing admin actions must use existing admin authentication/authorization and anti-forgery protections.

## Hosting Plan

Start local-first:

- Run `QueenZone.NewsAgent.Worker` with the `discover-news` command (see `docs/architecture/news-agent.md`).
- One-time OpenRouter setup: `src/QueenZone.NewsAgent.Worker/appsettings.Local.json` (from `appsettings.Local.json.example`).
- Windows smoke test: `scripts/Smoke-NewsAgent.bat` (fetch + triage; reports OpenRouter pass separately from feed errors).
- Support fetch-only, dry-run, AI-enabled, and full candidate-generation modes via worker flags.
- Document Windows Task Scheduler setup for local scheduled runs. *(not documented yet — #107)*

Production path:

- Prefer an Azure Function timer or isolated scheduled worker when dependable automation is needed.
- An App Service WebJob is also plausible if it fits the existing deployment shape.
- If hosted inside the web app, add a singleton/locking guard so scaled-out instances do not process the same candidates concurrently.

Secrets belong only in ignored local config or Azure configuration:

- `OpenRouter:ApiKey` in `src/QueenZone.NewsAgent.Worker/appsettings.Local.json`, or `OPENROUTER_API_KEY` env var
- Any future source-specific credentials, if ever needed.

See `.env.example` and `docs/architecture/news-agent.md`.

## Testing Expectations

Follow `docs/architecture/testing-policy.md`.

Default tests must not require live source access or OpenRouter credentials.

Use:

- Unit tests for canonical URL normalization, dedupe keys, source classification, status transitions, and structured AI output parsing.
- Web integration tests for admin access control and public unpublished-content isolation.
- Worker tests with fake fetchers and fake AI clients.
- Optional/manual real-source smoke checks that are clearly reported when skipped. Use `scripts/Smoke-NewsAgent.bat` on Windows.

Before a pull request, run the usual gate:

```powershell
dotnet restore QueenZone.sln
dotnet build QueenZone.sln --configuration Release --no-restore
dotnet test QueenZone.sln --configuration Release --no-build
```

## Key Safety Rules

- Do not commit secrets.
- Do not scrape private, deleted, hidden, moderated, or logged-in content.
- Do not copy full articles or long passages.
- Keep source links and provenance with every candidate and draft.
- Public pages must show only approved/published items.
- Visitor-facing and admin pages should be Razor Pages, not inline HTML streamed from minimal route handlers.
- Report whether real-source and real-OpenRouter checks were run or skipped.

## Suggested Agent Starting Prompt

```markdown
Resume the News Agent MVP in QueenZone.Modern.

Read:
- AGENTS.md
- docs/architecture/news-agent.md
- docs/backlog/news-agent-mvp-handoff.md
- docs/architecture/testing-policy.md
- The GitHub issue you are implementing under milestone `News Agent MVP`.

Use branch format `{agent}/{task}`.

Keep automated discovery and AI drafting behind human editorial approval. Public pages must render only approved/published news. Use deterministic fake data in default tests, and report whether live source/OpenRouter checks were skipped.
```
