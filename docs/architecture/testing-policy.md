# Testing Policy

## Objective

QueenZone Modern should have enough automated coverage to make the migration safe without making everyday development slow.

The first release is archive-first and visitor read-only, with news as the first protected editorial workflow. The highest-risk areas are:

- Legacy data mapping.
- Stable canonical URL behavior.
- Public/hidden content rules.
- News publication safeguards.
- Unsafe legacy HTML rendering.
- Deployment and configuration drift.
- Crawlable public pages.

Use a layered test suite. Keep the default local and CI test path fast, deterministic, and independent of the restored legacy database.

## Test Layers

### Unit Tests

Use unit tests for pure logic with no web host, filesystem, network, or database dependency.

News agent library unit tests (discovery, triage, draft generation, OpenRouter, SSRF URL safety) live in `tests/QueenZone.NewsAgent.Tests`. Web HTTP integration, Razor composition, and admin UI tests stay in `tests/QueenZone.Web.Tests`. Prefer `QueenZoneWebApplicationFactory` for Testing-environment hosts instead of re-applying `UseEnvironment("Testing")` in every class.

Good targets:

- Slug generation.
- Canonical route generation.
- Canonical URL parsing.
- Pagination calculations.
- Content visibility rules such as `DISPLAY = 1`.
- Editorial publication rules for new news items.
- Date and metadata formatting helpers.
- HTML sanitisation helpers when they are introduced.

Unit tests must run on every build and in every pull request.

Code coverage is reported for the default automated test suite. Treat coverage as a review signal, not as a replacement for useful assertions. New or changed pure logic should normally include targeted unit coverage, especially for canonical routes, pagination, visibility rules, date formatting, and HTML sanitisation.

### Web Integration Tests

Use ASP.NET Core integration tests for route and page behavior. These tests should use fake, in-memory, or sample repositories by default so they can run in CI without SQL Server.

Good targets:

- `/` renders latest news.
- `/news` renders the news archive.
- `/news/{id}/{slug}` renders detail pages.
- Wrong slugs redirect to canonical slugs.
- Missing or hidden records return 404.
- Canonical URLs are emitted in page links.
- Basic health and error behavior.

These tests are the default place to cover user-visible route behavior.

**Output cache:** environment `Testing` disables public HTML output caching so cases stay deterministic. Production-shaped hit/miss coverage lives in `PublicOutputCacheTests` (uses `UseEnvironment("Production")` plus the Entra host settings helper from `ResponseCompressionTests`). No extra secrets or env vars are required for a normal `dotnet test` run.

Admin editorial routes also have a second HTTP integration layer that wires `EfAdminNewsRepository` and `EfNewsDiscoveryRepository` through SQLite (`AdminNewsEfRoutesTests`, `AdminNewsDiscoveryEfRoutesTests`). Use that layer for create/edit/publish/promote persistence checks that in-memory fakes cannot catch.

Negative antiforgery coverage belongs in the default HTTP integration suite: at least one admin news action and one discovery action should return `400` when `__RequestVerificationToken` is missing.

#### Behavior-first HTML assertions

For route/page integration tests, prefer assertions that change only when behavior changes.

- Prefer status codes, redirect targets, canonical links, and user-visible domain text.
- Prefer model/repository state checks after POST actions when possible.
- Avoid assertions on CSS class names unless the class itself is a product contract.
- Avoid exact raw markup snapshots for common tags (`<title>`, exact anchor shape, container `<div>` structures).
- If structure matters, parse and assert semantically (or use a shared helper) instead of brittle raw string fragments.

Examples:

- Avoid: `Assert.Contains("archive-pagination-prev is-disabled", body);`
- Prefer: `Assert.DoesNotContain(TestSiteConfiguration.PrevLink("/news"), pageOne);`

- Avoid: `Assert.Contains("<title>QueenZone news &#x2013; Page 2</title>", body);`
- Prefer: `TestHtmlAssertions.AssertPageTitle(body, "QueenZone news – Page 2");`

Exception: exact markup assertions are still appropriate when markup sanitization or security output is the contract under test (for example, ensuring disallowed attributes are stripped).

### Data Integration Tests

Use data integration tests for the real restored legacy SQL Server database. These are opt-in unless a controlled test database is available in CI.

Good targets:

- `NEWS_T` queries return only published rows.
- Archive ordering matches the intended legacy behavior.
- Direct SQL and stored procedure mappings populate the modern read models correctly.
- Raw SQL projections over legacy tables materialize successfully against SQL Server column types.
- Nulls, unusual characters, and legacy HTML do not crash the mapping layer.
- Oldest, newest, and sample records can be loaded for each migrated content area.

Gate these tests behind explicit configuration such as:

```text
RUN_LEGACY_DB_TESTS=true
ConnectionStrings__QueenZoneLegacy=...
```

Do not require these tests in normal CI until the project has a known, repeatable test database.

The legacy probes run automatically each night through
`.github/workflows/nightly-legacy-checks.yml`. This is separate from normal CI: it is not a PR gate,
does not block merges, and runs only on a schedule or through `workflow_dispatch`.

They run against a same-day SQL Express mirror on the self-hosted Windows runner.
`scripts/Sync-LegacyDbToSqlExpress.ps1` refreshes it from live Azure SQL via a `sqlpackage`
extract/publish. Read probes then run from the macOS runner over the LAN. Self-cleaning write probes
run locally on Windows after the read checks pass:

| Probe surface | How nightly runs it |
| --- | --- |
| `EfAdminNewsRepositoryLegacyProbeTests` | Mac `legacy-read-probes` |
| `EfNewsSectionLiveProbeTests` public read Fact | Mac `legacy-read-probes` |
| `EfAdminNewsRepositoryLegacyWriteProbeTests` | Windows `legacy-write-probes` via `Probe-AdminNewsLegacyWrites.ps1` |
| `EfNewsSectionLiveProbeTests` `Admin_news_*` write Facts | Same script (rollback visibility + full lifecycle) |
| `EfNewsDiscoveryPromotionLiveProbeTests` | Same script (self-seeds disposable candidate, promote inside rollback, then deletes seed) |
| URL ingestion / private messaging / forum / content submission (incl. photo promotion → `PIC_FILES_T`) / member-account write probes | Dedicated Windows probe scripts |
| Real-data Playwright UI suite (see "Nightly UI Regression (Real Data)" below) | Windows + Mac `ui-e2e-realdata` via `scripts/Run-E2E.ps1 -Mode RealData` |

Their scripts reject Azure SQL, remote servers, and databases other than `queenzone_legacy_sync`. A
final marker scan fails the workflow if probe or leaked web-test residue remains. The optional full
URL fetch probe is manual through the workflow's `run_full_url_probe` input.

### Modern-Schema SQL Server Tests

`tests/QueenZone.SqlServerTests` covers modern EF-managed query paths that only run against a real
SQL Server — most commonly because EF Core's SQLite provider (used by the default `QueenZone.Web.Tests`
suite) cannot translate `DateTimeOffset` comparisons at all, not just in complex queries. Repositories
split those methods into an in-memory SQLite fallback (covered by `QueenZone.Web.Tests`, matches
default CI behavior) and a separate SQL-Server-only method with a short comment pointing here — see
`EfNewsSuggestionRepository.GetDashboardCountsViaSqlAggregateAsync` for the pattern.

Like `tests/QueenZone.Web.E2E`, this project is **not part of `QueenZone.sln`**, so the default
`dotnet test QueenZone.sln` (the local equivalent of the CI gate) stays DB-free. Unlike the opt-in
legacy data integration tests above, it does run on **every pull request** in CI — as its own job
(`sql-server-tests` in `.github/workflows/ci.yml`) against a throwaway `mcr.microsoft.com/mssql/server`
Docker service container, not the shared Azure SQL instance used by `ef-migrations`/deploy. Its
coverage is merged into the same union report as the `test` matrix shards, so these methods count
toward both coverage gates like any other code.

Run it locally against SQL Server LocalDB (no container needed):

```powershell
dotnet test tests/QueenZone.SqlServerTests/QueenZone.SqlServerTests.csproj
```

Set `ConnectionStrings__SqlServerTest` to point at a different SQL Server instance (for example the
CI container's `Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=True`) if LocalDB
isn't available. Each test creates and drops its own uniquely named scratch database, so tests can run
in parallel and never touch the legacy or Azure SQL databases.

When EF Core `SqlQueryRaw` maps legacy columns into typed row classes, do not rely only on in-memory route tests. The legacy schema uses many `smallint` and `bit` columns; SQL Server returns those as `System.Int16` and `bool`, not `int`. Either cast projected values to the row model type in SQL (for example, `CAST(Q_LINK_CAT_ID AS int) AS CategoryId`) and cover that SQL shape with a deterministic test, or run an opt-in read-only legacy database probe before deployment to prove the projection materializes.

For admin write checks, run `scripts/Probe-AdminNewsLegacyWrites.ps1` with `ConnectionStrings__QueenZoneLegacy` pointing to the local `queenzone_legacy_sync` SQL Express mirror and `RUN_LEGACY_WRITE_PROBE=true`. The script refuses other targets. It runs `EfAdminNewsRepositoryLegacyWriteProbeTests`, the `EfNewsSectionLiveProbeTests.Admin_news_*` write Facts, and the self-seeding `EfNewsDiscoveryPromotionLiveProbeTests` (promote inside a rolled-back transaction, then delete seed rows).

For private messaging IDENTITY `SortKey` and conversation write-lock checks, use the same SQL Express mirror with `scripts/Probe-PrivateMessaging.ps1` and `RUN_PRIVATE_MESSAGE_PROBE=true`. The script refuses Azure SQL and remote servers. The probe creates throwaway members, exercises concurrent first-sends and replies, asserts tip `LastMessageSortKey` consistency, and deletes the probe rows.

For modern forum thread/post writes (SQL Server sequences + read stats), use `scripts/Probe-ForumWrites.ps1` with `RUN_FORUM_WRITE_PROBE=true`.

For photo and article submission lifecycles (including photo-submission promotion into legacy `PIC_FILES_T` / `PIC_CAT_T`), use `scripts/Probe-ContentSubmissions.ps1` with `RUN_CONTENT_SUBMISSION_PROBE=true`. The promotion Fact self-seeds a disposable submission, writes a visible gallery row through `EfAdminPhotoRepository.CreateAsync` + `EfPhotoSubmissionRepository.PromoteAsync` (the repository path used by `PhotoSubmissionPromotionService`, without blob copy), asserts the `PIC_FILES_T`/`PIC_CAT_T` join, and deletes probe rows.

For member account create + external login, use `scripts/Probe-MemberAccounts.ps1` with `RUN_MEMBER_ACCOUNT_PROBE=true`.

For admin URL ingestion and run-request queue checks, use the same SQL Express mirror with `scripts/Probe-NewsAgentUrlIngestion.ps1` and `RUN_NEWS_AGENT_URL_INGESTION_PROBE=true`. Default mode exercises SQL queue, claim, and completion. Pass `-Full` to fetch a public URL and triage through the local worker stack; an optional `OPENROUTER_API_KEY` enables AI triage. Both modes delete their requests, heartbeats, and related discovery records before returning. The full mode never publishes.

### Migration And Content Validation

Each migrated content area needs validation beyond ordinary unit tests.

For every content area, produce a repeatable validation check or report that covers:

- Count of public legacy records.
- Count of rendered, imported, or projected modern records.
- Canonical URL coverage.
- Broken internal links and media references.
- Encoding and legacy HTML edge cases.
- Private, hidden, deleted, or moderated fields are not exposed.
- Spot checks for oldest, newest, and random records.

These checks may live as test projects, import-tool reports, or validation scripts, but the output must be readable enough to support a release decision.

### End-To-End Tests

Use Playwright for a small browser-level smoke suite once the UI has stable pages.

The suite lives in `tests/QueenZone.Web.E2E` (not part of `QueenZone.sln`, so default `dotnet test QueenZone.sln` stays DB-free and server-free).

Good targets (covered or expanding):

- Homepage, news archive (including pagination), and news detail (canonical + body).
- Forum index, category, and topic (posts + breadcrumbs).
- Articles, biography, photography, and search surface loads.
- Mobile viewport + open mobile nav menu.
- axe-core accessibility smoke: **critical** violations fail the run (serious findings are logged).
- Admin news list and create-draft flow with `X-Test-User-Email` test auth in the `Testing` environment.
- Editorial discovery promote → publish → public visibility journey.

Keep the PR-gate end-to-end suite small. It should prove critical user journeys and browser behavior, not duplicate all route integration tests, and it must stay small, deterministic, and in-memory — see "Nightly UI Regression (Real Data)" below for the separate tier that intentionally trades that speed for real-data coverage.

On failure, tests write screenshots and Playwright traces under `test-results/e2e/` (gitignored). CI uploads that folder as an artifact when the e2e job fails.

### Nightly UI Regression (Real Data)

> **Nightly UI regression (real data)** — extensive browser coverage against the SQL Express mirror in the `E2E` environment. Not a PR gate. Assertions must be shape-based, not content-based, because mirror data changes nightly. All writes must be marked `uie2e-` and self-cleaning, and covered by `EfLegacyProbeResidueTests`. The PR-gate e2e suite stays small, deterministic, and in-memory.

This is a second, separate layer from the End-To-End Tests above, sharing the same `tests/QueenZone.Web.E2E` project but running under the `E2E` hosting environment (real SQL Express mirror + test auth, see `AGENTS.md`) instead of `Testing` (in-memory). It runs nightly through `.github/workflows/nightly-legacy-checks.yml`, not on pull requests.

`SitemapPublicRouteSweepTests` (`[Category("RealData")]`, `[Category("ReadOnly")]`) discovers URLs at runtime from `/sitemap.xml` (sampling first/last/seeded-random per section, capped and logged for reproducibility), plus a fixed list of routes not in the sitemap, and asserts page *shape* rather than content — HTTP 200, a single non-empty `<h1>`, a single matching canonical link, no console errors, no horizontal overflow at 390px, and no unrendered HTML-encoding artifacts. It also checks the negative cases (unknown path → styled 404, stale slug → redirect) and runs the axe-core critical-violation check on one representative page per section. It performs no writes and passes with `E2E_READONLY=true`, so the live-site job can reuse it unchanged.

**Live-site read-only public sweep** (`.github/workflows/livesite-readonly-sweep.yml`, issue #551): its own scheduled workflow (06:00 UTC daily, plus `workflow_dispatch`) on the Mac runner against `https://www.queenzone.org` via `scripts/Run-E2E.ps1 -Mode LiveSite`. Separate from `nightly-legacy-checks.yml` so it needs no database and does not wait on mirror sync. That mode sets `E2E_READONLY=true`, filters to `TestCategory=RealData&TestCategory=ReadOnly` (currently `SitemapPublicRouteSweepTests` + `LiveSiteMediaCdnTests` + `LiveSiteContentApiTests`), refuses a localhost base URL, and caps NUnit to one worker so production rate limiting is not tripped. Write-capable RealData fixtures throw at setup under `E2E_READONLY` (`RealDataWriteGuard`). Failures are production/CDN signal (messages prefix `PRODUCTION LIVE-SITE`), not mirror/code-path signal — continuous only, never a PR gate.

`LiveSiteContentApiTests` is an HTTP (not Playwright page) shape sweep of the public mobile JSON API: discovery document, OpenAPI, `/api/v1/content/*` list envelopes, one detail per resource using an id from that list (never hardcoded), and Problem Details 404 for an unknown `/api/v1` path. It does not call `/api/v1/auth` or `/api/v1/admin`. The same fixture also runs in the nightly RealData suite against the SQL Express mirror. In-memory contract tests stay in `QueenZone.Web.Tests` (`ApiV1RoutesTests`, `ContentApi*Tests`). Post-deploy smoke (`scripts/Smoke-LiveSite.ps1` / `scripts/Invoke-PostDeploySmoke.sh`) hits `GET /api/v1` and one content list as a cheap deploy canary.

`CommunitySubmissionWorkflowTests` (`[Category("RealData")]`, write-capable — **not** `ReadOnly`) covers the member-facing content submission pipeline against the SQL Express mirror: photo submission (title, description, category, upload) through to a Pending status badge on `/account/my-submissions`; article submission through the shared Quill editor (`Shared/_RichTextEditor.cshtml`) including an attached cover image, through to a Submitted status; news suggestion submission through to the admin suggestion queue (`/admin/news-suggestions`) as Pending; an account settings display-name change that persists across a subsequent submission; and one validation case per form (missing required field or wrong-type upload), asserted on the user-visible `asp-validation-for` copy. It seeds a real `MemberAccounts` row per test via direct EF access (`QueenZone.Data`, referenced only by the E2E project, not the web app) before impersonating that member with `X-Test-Member-Id`/`-Name`/`-Email`, since the submission and account pages look the member up through `MemberAccountService`/the submission repositories rather than trusting the test-auth claim alone. Every row it creates carries a `uie2e-{runId}-{fixture}-{n}` marker (`RealDataMarkers`/`RealDataPageTest`); teardown deletes them via the same EF context, and `EfLegacyProbeResidueTests` in `QueenZone.Web.Tests` also scans for that marker as a nightly backstop.

Run it on demand with `scripts/Run-E2E.ps1 -Mode RealData` (requires `ConnectionStrings__QueenZoneLegacy` pointing at the local SQL Express mirror; see `docs/architecture/self-hosted-e2e-runner.md`). Nightly runs it on both self-hosted runners (`ui-e2e-realdata` job, one shard per OS) so the UI suite itself gets coverage on both operating systems, even though the mirror database only lives on the Windows box.

#### Selector conventions

The [behavior-first principle](#behavior-first-html-assertions) for HTML integration tests applies to Playwright locators too, with one addition specific to browser-level tests: prefer, in order —

1. **`GetByRole`** (with an accessible name) — matches how a user or assistive tech finds the element and survives markup/styling refactors.
2. **`data-testid`** — add one to the markup when role-based targeting isn't specific enough (e.g. picking one of several similar rows, or targeting a third-party widget's internals). This is a project-owned contract: renaming or removing it is a deliberate, reviewable change, unlike a CSS class that can shift for styling reasons alone.
3. **Project-owned CSS classes** (e.g. `qz-*`, `admin-*`) — acceptable when the class is effectively a stable hook, but prefer promoting it to a `data-testid` if the test starts chaining `.Filter(HasText: ...)` or nested class selectors to disambiguate.

Avoid:

- Selecting on **third-party library internals** (for example Quill's own `.ql-editor` class). A library upgrade can rename or restructure these with no relation to a code change in this repo. Tag the element you actually need with `data-testid` instead — see `quill.root.setAttribute("data-testid", "rich-text-editor")` in `wwwroot/js/editor/rich-text-editor.js`, used by `Page.Locator("[data-testid='rich-text-editor']")` across `tests/QueenZone.Web.E2E`.
- **Full-sentence `GetByText` matches on product copy** (e.g. a whole validation or confirmation message). A copy edit unrelated to the behavior under test then breaks the test. Prefer matching a shorter, structural fragment, or asserting via a `data-testid`d status element instead of the sentence itself.
- Deeply chained locators that combine several fragile signals at once (class → text filter → nested class) — each link is a separate way for the test to break for reasons unrelated to the behavior it's meant to prove.
- **Substring accessible-name matches on short words that also appear in the chrome.** Playwright's `GetByLabel("Message")` and `GetByRole(..., Name = "Message")` match `aria-label="Messages"` (and `Messages, N unread conversations`) unless you pass `Exact = true`. The signed-in masthead always exposes that control, so compose/reply fields must use a unique role — typically `GetByRole(AriaRole.Textbox, new() { Name = "Message", Exact = true })` — not `GetByLabel("Message")`. The PR-gate Deterministic suite never renders the compose textarea next to that masthead icon (no in-memory recipient), so this class of collision only shows up in nightly RealData. After changing RealData locators, dispatch `nightly-legacy-checks.yml` with `skip_sync=true` and `category_filter` set to the fixture instead of waiting for 03:00 UTC.

### Frontend performance checks (advisory)

Use Lighthouse via `scripts/Measure-FrontendPerformance.ps1` when a change may affect end-user load cost on public pages (homepage, news, forum).

**How often:** not every PR and not daily CI. Run **before/after** frontend or static-asset changes; optionally after deploys that touch the public shell; optionally about **quarterly** for drift. Skip pure backend/docs/test work. Full cadence table: `docs/performance/frontend-performance-checks.md` (section **When to run**).

Good targets:

- LCP, CLS, total transfer size, and request count on key public routes.
- Before/after summaries attached to performance-related pull requests.
- Optional repeat-load pass when validating cache headers or static asset changes.

These checks are **opt-in and advisory**. They are not a merge-blocking CI gate. Documented budgets live in `docs/performance/frontend-performance-budgets.json`. Workflow detail: `docs/performance/frontend-performance-checks.md`.

```powershell
powershell -File .\scripts\Measure-FrontendPerformance.ps1 -StartLocalApp -FormFactor mobile
```

## Continuous Integration

Every pull request must run (local equivalent of the CI gate):

```powershell
dotnet restore QueenZone.sln
dotnet build QueenZone.sln --configuration Release --no-restore
dotnet format QueenZone.sln --verify-no-changes
dotnet test QueenZone.sln --configuration Release --no-build
```

CI enforces formatting against the root `.editorconfig` via `dotnet format … --verify-no-changes` in its own `format` job (runs in parallel with `build`/`test`, not as a step inside `build` — see "Other CI jobs" below). If that step fails, run `dotnet format QueenZone.sln` locally and commit the result.

Line endings: `.editorconfig` requires CRLF. Root `.gitattributes` sets `* text=auto eol=crlf` so Linux CI and Windows agents share the same working-tree endings. Without that, Linux checkouts stay LF and fail `ENDOFLINE` while Windows with `core.autocrlf=true` stays green.

CI also collects coverage from the deterministic test suite (merged across Web.Tests shards) and publishes an HTML/Cobertura report artifact. The coverage report is expected to help reviewers spot untested risk.

### CI test sharding (Web.Tests)

`QueenZone.Web.Tests` dominates suite wall-clock (~85%). CI runs it as **mixed shards** in parallel so each GitHub-hosted runner keeps a blend of light unit tests and heavier `WebApplicationFactory` tests.

| Piece | Role |
| --- | --- |
| `scripts/Get-WebTestShardFilter.ps1` | Discovers `*Tests` classes, assigns them with greedy weight balance (case count × host kind: EF-WAF 20, Production WAF 10, other WAF 5, SQLite unit 2, unit 1), emits an xUnit `--filter` |
| `scripts/Invoke-WebTestsShard.ps1` | Runs one shard's filtered Web.Tests (`-SmallProjectsOnly` runs just the Tools/Storage/NewsAgent projects instead) |
| `.github/workflows/ci.yml` jobs `test` + `small-projects-tests` + `coverage` | Matrix `shard: [0, 1]` for Web.Tests, a separate parallel job for the small projects, then merge Cobertura and run the coverage gate |

The `build` job uploads `src/**/bin/Release`, `tests/**/bin/Release`, and `src/QueenZone.Web/obj/Release`. Keep PDBs — Coverlet maps executed lines from them, so a `--no-build` shard without symbols collapses global coverage. Keep `*.xml` — NewsAgent tests copy fixture XML into the output directory and `--no-build` shards read those files from disk. Shards must keep the QueenZone.Web `obj` tree — ASP.NET Core’s `WebApplicationFactory` resolves compressed static web assets under `src/QueenZone.Web/obj/.../compressed/`. Uploading only `bin` causes `DirectoryNotFoundException` in Development-environment host tests (for example `StaticAssetCacheHeadersTests`). Other project `obj` trees are not required for `--no-build` shard runs.

**Do not** split CI as “all unit tests in job A / all WAF integration tests in job B”. That was measured in [#442](https://github.com/richardorchard/QueenZone.Modern/issues/442) and **regressed** wall-clock: isolating every `WebApplicationFactory` host onto one runner increases contention, and that job became slower than the old single-suite run. Mixed shards are required.

**Local development:** keep using `dotnet test QueenZone.sln` (full suite, no filter). Sharding is a CI wall-clock optimization, not a new project layout. To inspect or time shards locally:

```powershell
powershell -File ./scripts/Get-WebTestShardFilter.ps1 -SelfTest
powershell -File ./scripts/Get-WebTestShardFilter.ps1 -ShardCount 2 -List
dotnet build QueenZone.sln --configuration Release
powershell -File ./scripts/Invoke-WebTestsShard.ps1 -ShardIndex 0 -ShardCount 2 -NoBuild -NoRestore
powershell -File ./scripts/Invoke-WebTestsShard.ps1 -ShardIndex 1 -ShardCount 2 -NoBuild -NoRestore
powershell -File ./scripts/Invoke-WebTestsShard.ps1 -SmallProjectsOnly -NoBuild -NoRestore
```

(issue #496: the small projects used to ride along on shard 0, making it consistently slower than shard 1 even though the Web.Tests weight split itself was even. `-SmallProjectsOnly` now runs them as CI's own parallel `small-projects-tests` job instead. A later even class-weight split still parked every `Admin*EfRoutes` host on shard 1 because they all had weight 5 and sorted together; case-count × kind multipliers exist so those hosts spread.)

When adding Web.Tests classes: no shard manifest to update — discovery is automatic. Prefer `QueenZoneWebApplicationFactory` for HTTP tests; keep true unit tests free of `WebApplicationFactory` so they stay cheap filler in every shard.

If CI wall-clock grows again, prefer (in order): thin theory-heavy smoke HTTP tests; raise `ShardCount` / matrix size with the same mixed algorithm; paid larger runners. Avoid unit-vs-WAF project splits and raising xUnit `maxParallelThreads` (more threads worsened contention in #442). Smaller parked ideas (format `--include`, EF migrations bundle, extra shards today) live in [#657](https://github.com/richardorchard/QueenZone.Modern/issues/657).

### Coverage gates (enforced on every pull request)

Implemented in `scripts/Test-CoverageGate.ps1` and invoked from the `coverage` job in `.github/workflows/ci.yml` after all `test` matrix shards finish. The gate unions every `coverage.cobertura.xml` under the downloaded results (see the script’s union logic).

| Gate | Threshold | What it measures |
| --- | --- | --- |
| **Global line coverage** | **≥ 51%** | Line coverage across the union of Cobertura reports from all shards / test projects |
| **Changed-line coverage** | **≥ 70%** | Coverable `.cs` lines added or modified in the PR diff against the base branch (`main`) |

Rules:

- Changed-line coverage is computed from `git diff origin/main...HEAD` for `*.cs` files only.
- Only lines that appear in the Cobertura report count as coverable. Non-executable lines, some boilerplate, and excluded files do not count.
- If a pull request changes no coverable C# lines, the changed-line gate is skipped.
- `coverlet.runsettings` excludes `**/obj/**/*.cs` and `**/Migrations/**/*.cs` from coverage collection.

These gates are guardrails, not a replacement for useful assertions. New or changed pure logic should still normally include targeted unit coverage, especially for canonical routes, pagination, visibility rules, date formatting, and HTML sanitisation.

### Other CI jobs

| Job | Purpose | Blocks merge? |
| --- | --- | --- |
| `build` | Restore, build, upload binaries + Linux publish artifact | Yes |
| `format` | `dotnet format --verify-no-changes`, runs in parallel with `build`/`test` instead of blocking artifact upload | Yes |
| `test` | Mixed Web.Tests shards with Coverlet | Yes |
| `small-projects-tests` | Tools/Storage/NewsAgent test projects, in parallel with the `test` shards | Yes |
| `sql-server-tests` | `QueenZone.SqlServerTests` against a Docker `mssql` service container | Yes |
| `coverage` | Merge shard + SQL Server + small-projects Cobertura reports, HTML summary, coverage gates | Yes |
| `ef-migrations` | When migration-related paths change: snapshot check + `database update` on Azure SQL | Yes (same-repo PRs only; skipped otherwise) |
| `smoke-test` | Published app, curl `/health`, `/`, `/news` (starts after `build`, overlaps shards/coverage) | Yes |
| `e2e-test` | Playwright suite on a self-hosted `e2e` runner (Windows or macOS; starts after `build`, overlaps coverage) | Yes (required PR merge gate) |
| `mobile-js` | `npm ci` + `npm run preflight` in `src/QueenZone.Mobile` (typecheck, discovered unit tests, pinned Expo Doctor) | Yes — required on `main` after #870; skip-success stub when that tree is unchanged |
| `mobile-android` | Unsigned debug APK compile (GitHub-hosted Linux) | Yes — required on `main` after #870; skip-success stub when that tree is unchanged |
| `mobile-ios` | Unsigned Simulator compile (GitHub-hosted macOS) | Yes — required on `main` after #870; skip-success stub when that tree is unchanged |
| `mobile-api-contracts` | Testing-host consumer contracts: real `/api/v1` responses through the mobile `fetchJson` / domain clients plus runtime zod schemas (#869 Option A) | Independent of native jobs; skip-success stub when contract paths are unchanged |

Local mobile validation from `src/QueenZone.Mobile` is a clean `npm ci` then `npm run preflight`. `npm test` discovers every `src/**/*.test.ts` and `src/**/*.test.tsx` file (no package.json path list) and self-checks that unlisted Node and Jest probes still run. Pure `*.test.ts` files use Node's test runner; `*.test.tsx` files use Jest + `jest-expo` + React Native Testing Library (no devices, Metro, or production services). `npm run preflight` is typecheck + those tests + `npm run doctor` (lockfile-pinned `expo-doctor`). Device E2E remains a separate track (#872).

These four **GitHub check names** (the job `name:` values in `ci.yml`) must be required contexts on protected `main`. A workflow file cannot enable branch protection; a human with repo admin access has to add them after this change merges:

- `Mobile typecheck and unit tests`
- `Mobile Android build`
- `Mobile iOS build`
- `Mobile API consumer contracts`

**Live `main` required contexts** (queried 2026-08-24; mobile names are **not** in this list yet): `build`, `test (0)`, `test (1)`, `sql-server-tests`, `coverage`, `smoke-test`, `e2e-test`, `Verify formatting`, `Small test projects (Tools/Storage/NewsAgent)`. Do not treat YAML as proof that mobile checks are required. After merge, add the four mobile names above and re-query Settings → Branches → `main` → Status checks to confirm.

Android and iOS are equal: a mobile PR cannot treat either native compile as optional. Non-mobile PRs are not left pending: `ci.yml` emits skip-success stubs (`mobile-js-ok`, `mobile-android-ok`, `mobile-ios-ok`) with those exact check names, the same idea as `test-docs-ok`. `mobile-api-contracts-ok` is the matching stub for `Mobile API consumer contracts`.

**Layers (do not collapse these):**

```text
unit (npm test / Web.Tests)
  ≠ consumer contracts (Testing host + real mobile parsers)
  ≠ native compile (mobile-android / mobile-ios)
  ≠ device smoke (#872)
```

Node + Jest tests (`npm test`, fast, no native toolchain) are not a substitute for `mobile-android` / `mobile-ios` compile, and those unsigned CI compiles are not device/E2E coverage. Static TypeScript and generated OpenAPI → TS types are not a substitute for the consumer-contract suite: `src/api/client.ts` uses `fetch` plus `as T`, so a renamed JSON field still typechecks. Device E2E is a separate track (#872).

Local consumer contracts (no secrets, no real database; run twice to prove determinism):

```powershell
# Linux / macOS / GitHub Actions
bash ./scripts/run-mobile-api-contracts.sh

# After a Release web build already exists:
bash ./scripts/run-mobile-api-contracts.sh --no-build
```

The script starts `QueenZone.Web` with `ASPNETCORE_ENVIRONMENT=Testing` and `QUEENZONE_MOBILE_CONTRACT_HOST=1` on a loopback ephemeral port, then runs `npm run test:api-contracts` in `src/QueenZone.Mobile`. Failures name the endpoint and expected field or status. A renamed server JSON property (for example `NewsListItemDto.Title` → `Headline`) or a tightened consumer schema (for example `title: z.number()`) must fail with a message such as `Contract GET /api/v1/content/news failed: items.0.title: ...`. Revert those probes; do not commit them.

**Publish preflight:** `.github/workflows/publish-mobile-test-build.yml` and `.github/workflows/publish-ios-testflight.yml` run `npm ci` + `npm run preflight` against `github.sha` in a job with no signing secrets. The signing/upload job `needs` that preflight and runs only when `needs.preflight.result == 'success'`. A failed or cancelled preflight skips publication.

CI/CD uses two workflows. `.github/workflows/ci.yml` runs the pull-request build, deterministic tests, coverage gates, conditional `ef-migrations`, smoke test, and required e2e merge gate. After merge, `.github/workflows/deploy.yml` resolves the `ci.yml` run that built and tested the merged PR's head commit (via merge-commit second parent, or the commit→PR association for squash/rebase merges) and reuses its `web-publish` artifact (no rebuild), then runs `migrate` (only when EF paths changed) → `deploy` (zip-pushes, Kudu recycle, polls `/warmup` **and** the new `data-build-version` on `/`) → `post-deploy-smoke`. Resolution keys off a non-expired `web-publish-*` artifact for that head SHA (`scripts/Resolve-CiPublishRun.sh`), not overall workflow `conclusion == success`: mixed web+mobile PRs keep `ci.yml` in_progress on native Mobile iOS/Android builds after required web checks (and often merge) already passed, which previously failed deploy on #860 / #866 even though the zip existed. ARM `WEBSITE_RUN_FROM_PACKAGE=1` does swap the zip, but #688 showed that skipping the extra Kudu recycle leaves `/warmup` on HTTP 500; keep the restart. Skipping migrate must not skip smoke: `post-deploy-smoke` uses `if: always()` and requires `deploy` to have succeeded. Smoke also requires `data-build-version` on `/` to match the PR-head short SHA stamped at CI build (`OverrideGitCommitShort`). The PR `ef-migrations` job uses the same migration connection string as deploy so SQL Server failures are caught before merge.

Two further workflows run on a schedule only and never gate a PR merge or a deploy: `.github/workflows/nightly-legacy-checks.yml` (legacy read/write probes, then the real-data Playwright UI suite, then a residue check — see "Data Integration Tests" and "Nightly UI Regression (Real Data)" above) and `.github/workflows/livesite-readonly-sweep.yml` (the live-site read-only sweep). Both are continuous signal for catching drift, not merge gates; a failure there does not block or revert anything automatically.

Pull requests that do not change the website skip `build` / `test` / coverage / smoke / e2e. Classification lives in `scripts/classify-pipeline-changes.sh`:

- **Non-web** when **every** changed file is under `docs/`, `infra/`, `design/`, `.github/` (except `.github/workflows/ci.yml`), a root `*.md`, `LICENSE`, `THIRD-PARTY-NOTICES.md`, or `src/QueenZone.Mobile/`.
- Changing `ci.yml` itself still runs the full .NET suite.
- `src/` (except the mobile client), `tests/`, `scripts/`, project files, and `wwwroot` stay on the full web path.
- A mobile-only PR still runs `mobile-js` (`npm run preflight`: typecheck, discovered unit tests, pinned Expo Doctor), plus `mobile-android` and `mobile-ios` native compile builds (unsigned debug APK / Simulator build, uploaded as 1-day workflow artifacts). Those three check names are intended to be required on `main` (#870); a human must enable them in branch protection after merge. Non-mobile PRs get skip-success stubs so they are not left pending.
- `mobile_api_contracts=true` is **independent of** `mobile=true`. Server-only `/api/v1` changes (and json-api docs, mobile `src/api` / config / session helpers, the contract host/scripts, or `ci.yml`) run `mobile-api-contracts` without Android/iOS native compilation. A UI-only mobile change still compiles native jobs and does **not** start the contract host.
- Mixed mobile + web PRs run both pipelines. Mixed API + mobile client PRs run contracts **and** native jobs.
- Deploy uses the same classifier so an infra-only or mobile-only merge does not zip-deploy unchanged website binaries. Mixed web+mobile merges still deploy the website; `resolve-ci-run` must not wait for Mobile iOS/Android to finish the overall `ci.yml` conclusion (see `scripts/Resolve-CiPublishRun.sh`).

Skipped non-matrix jobs still report under their required check names, which GitHub treats as satisfied. The `test` matrix is different: skipping it entirely would report a single `test` check and never create the required `test (0)` / `test (1)` checks, leaving the PR blocked forever. `ci.yml` therefore runs a lightweight `test-docs-ok` matrix on non-web PRs that emits success for those exact names without running the .NET suite. The three mobile jobs similarly emit skip-success stubs (`mobile-js-ok`, `mobile-android-ok`, `mobile-ios-ok`) on non-mobile PRs so required mobile contexts are not left pending. `mobile-api-contracts-ok` does the same for the `Mobile API consumer contracts` check name when contract paths are unchanged.

### EF migration consistency

When a change adds, removes, or changes an EF-mapped entity in `QueenZoneDbContext`, verify the model snapshot is current before opening the pull request:

```powershell
dotnet ef migrations has-pending-model-changes --project src/QueenZone.Data/QueenZone.Data.csproj --startup-project src/QueenZone.Web/QueenZone.Web.csproj
```

This check is required even when the migration itself is hand-written SQL. EF still compares the runtime model to `QueenZoneDbContextModelSnapshot` during `dotnet ef database update`; if the snapshot does not include the model change, deployment fails with `PendingModelChangesWarning`.

For hand-written idempotent SQL migrations, add the normal EF migration designer/snapshot metadata as well. If the SQL migration already performs the real DDL, the follow-up sync migration should be a deliberate no-op in `Up`/`Down` whose purpose is only to advance EF's model snapshot.

**SQL Server batch binding:** do not put `ALTER TABLE ... ADD column` and a later `CREATE INDEX` / `UPDATE` / DML that references that new column in the same `migrationBuilder.Sql(...)` string. SQL Server compiles the whole batch before execution and fails with error 207 (`Invalid column name`). Use a separate `migrationBuilder.Sql` call (separate batch) for each dependent step. Filtered indexes and `CREATE OR ALTER PROCEDURE` that need to avoid ambient transactions may still use `suppressTransaction: true` on their own call.

## Pre-pull request checklist

Before opening a pull request, run the full local gate—not only `dotnet test`:

```powershell
git fetch origin main
dotnet restore QueenZone.sln
dotnet build QueenZone.sln --configuration Release --no-restore
dotnet format QueenZone.sln --verify-no-changes
dotnet test QueenZone.sln --configuration Release --no-build --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory ./TestResults
powershell -File ./scripts/Test-CoverageGate.ps1 -Reports ./TestResults -GlobalLineThreshold 51 -ChangedLineThreshold 70 -BaseRef origin/main
```

If the pull request touches `QueenZoneDbContext`, entity mappings, or files under `src/QueenZone.Data/Migrations/`, also run:

```powershell
dotnet ef migrations has-pending-model-changes --project src/QueenZone.Data/QueenZone.Data.csproj --startup-project src/QueenZone.Web/QueenZone.Web.csproj

$env:ConnectionStrings__QueenZoneLegacy = "<migration connection string>"
dotnet ef database update --project src/QueenZone.Data/QueenZone.Data.csproj --startup-project src/QueenZone.Web/QueenZone.Web.csproj
```

CI will re-run both steps on Azure SQL for same-repo PRs. Prefer fixing failures there before merge rather than discovering them on deploy-to-`main`.

Use `pwsh` instead of `powershell` on Linux or macOS.

### When changed-line coverage fails

1. Read the script output. It prints `Changed-line coverage: X%` and up to 20 uncovered `file:line` entries.
2. Add tests that execute the uncovered paths. Prefer:
   - Unit tests for pure logic (no I/O).
   - Fake HTTP clients, in-memory repositories, or SQLite EF tests for data-access and service code.
   - Web integration tests for Razor route behavior.
3. Re-run the checklist until changed-line coverage is at least 70%.
4. Do not rely on live network, OpenRouter, or legacy SQL for default tests.

Optional manual checks (report skipped in PRs when not run):

- News agent OpenRouter smoke: `scripts/Smoke-NewsAgent.bat` (Windows). See `docs/architecture/news-agent.md`.
- Frontend performance (Lighthouse): `scripts/Measure-FrontendPerformance.ps1`. See `docs/performance/frontend-performance-checks.md`.

Common gaps: new repository implementations, console/worker entry points, DI registration-only code (cover via integration tests that resolve services), and error branches.

For local HTML coverage inspection:

```powershell
dotnet tool restore
dotnet test QueenZone.sln --configuration Release --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory ./TestResults
dotnet tool run reportgenerator -reports:".\TestResults\**\coverage.cobertura.xml" -targetdir:".\coverage-report" -reporttypes:"HtmlInline;Cobertura;MarkdownSummary"
```

Do not commit generated `TestResults/` or `coverage-report/` output.

Playwright browser smoke tests live in `tests/QueenZone.Web.E2E` and run in CI on whichever self-hosted runner carrying the `e2e` label is available (currently Windows or macOS), avoiding GitHub Actions minutes. This job is a required pull-request merge gate; the deploy workflow does not rerun it. See `docs/architecture/self-hosted-e2e-runner.md` for runner setup and operational notes.

## Test Selection Rules

- Pure logic belongs in unit tests.
- Route and page behavior belongs in web integration tests.
- SQL mapping belongs in opt-in data integration tests.
- Migration confidence belongs in content validation reports.
- Browser behavior belongs in a small, deterministic, in-memory Playwright end-to-end suite that runs as a PR gate.
- Extensive real-data browser coverage belongs in the nightly-only `E2E`-environment Playwright tier, never a PR gate.
- End-user load cost belongs in the advisory frontend performance workflow, not in every PR.

## Pull Request Expectations

Every pull request should state:

- What was changed.
- Which test layers were run.
- Whether legacy database tests were run or intentionally skipped.
- Any remaining manual checks.

If a change touches legacy data access, canonical routes, content rendering, or publication rules, it should include tests or validation evidence for the affected behavior.

Pull requests should mention any meaningful coverage movement when the change adds risky logic or intentionally leaves a path untested.
