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
- Nulls, unusual characters, and legacy HTML do not crash the mapping layer.
- Oldest, newest, and sample records can be loaded for each migrated content area.

Gate these tests behind explicit configuration such as:

```text
RUN_LEGACY_DB_TESTS=true
ConnectionStrings__QueenZoneLegacy=...
```

Do not require these tests in normal CI until the project has a known, repeatable test database.

For pre-release admin write checks against the configured legacy SQL Server database, run `scripts/Probe-AdminNewsLegacyWrites.ps1` with both `ConnectionStrings__QueenZoneLegacy` and `RUN_LEGACY_WRITE_PROBE=true` set. The probe creates, publishes, unpublishes, and deletes a uniquely named test article. Point the connection string at a database you are willing to mutate (often the same Azure SQL instance used locally or in production), not the in-memory sample data path.

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

Keep end-to-end tests small. They should prove critical user journeys and browser behavior, not duplicate all route integration tests.

On failure, tests write screenshots and Playwright traces under `test-results/e2e/` (gitignored). CI uploads that folder as an artifact when the e2e job fails.

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

Every pull request must run:

```powershell
dotnet restore QueenZone.sln
dotnet build QueenZone.sln --configuration Release --no-restore
dotnet test QueenZone.sln --configuration Release --no-build
```

CI also collects coverage from the deterministic test suite and publishes an HTML/Cobertura report artifact. The coverage report is expected to help reviewers spot untested risk.

### Coverage gates (enforced on every pull request)

Implemented in `scripts/Test-CoverageGate.ps1` and invoked from `.github/workflows/ci.yml` after tests complete.

| Gate | Threshold | What it measures |
| --- | --- | --- |
| **Global line coverage** | **≥ 51%** | Line coverage across the full Cobertura report from `QueenZone.Web.Tests` |
| **Changed-line coverage** | **≥ 80%** | Coverable `.cs` lines added or modified in the PR diff against the base branch (`main`) |

Rules:

- Changed-line coverage is computed from `git diff origin/main...HEAD` for `*.cs` files only.
- Only lines that appear in the Cobertura report count as coverable. Non-executable lines, some boilerplate, and excluded files do not count.
- If a pull request changes no coverable C# lines, the changed-line gate is skipped.
- `coverlet.runsettings` excludes `**/obj/**/*.cs` and `**/Migrations/**/*.cs` from coverage collection.

These gates are guardrails, not a replacement for useful assertions. New or changed pure logic should still normally include targeted unit coverage, especially for canonical routes, pagination, visibility rules, date formatting, and HTML sanitisation.

### Other CI jobs

| Job | Purpose | Blocks merge? |
| --- | --- | --- |
| `build` | Restore, build, test, coverage gates | Yes |
| `ef-migrations` | When migration-related paths change: snapshot check + `database update` on Azure SQL | Yes (same-repo PRs only; skipped otherwise) |
| `smoke-test` | Publish app, curl `/health`, `/`, `/news` | Yes |
| `e2e-test` | Playwright suite on self-hosted Windows runner | No (`continue-on-error` if runner offline) |

Merges to `main` also trigger `.github/workflows/deploy-app-service.yml`, which re-runs tests, applies EF Core migrations, and deploys to the dev App Service. That is separate from pull request checks. The PR `ef-migrations` job uses the same migration connection string so SQL Server failures are caught before merge.

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
dotnet test QueenZone.sln --configuration Release --no-build --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory ./TestResults
powershell -File ./scripts/Test-CoverageGate.ps1 -Reports ./TestResults -GlobalLineThreshold 51 -ChangedLineThreshold 80 -BaseRef origin/main
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
3. Re-run the checklist until changed-line coverage is at least 80%.
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

Add these checks when the project is ready:

```powershell
dotnet format QueenZone.sln --verify-no-changes
```

Playwright browser smoke tests live in `tests/QueenZone.Web.E2E` and run in CI on a self-hosted Windows runner to avoid consuming GitHub Actions minutes. See `docs/architecture/self-hosted-e2e-runner.md` for runner setup and operational notes.

## Test Selection Rules

- Pure logic belongs in unit tests.
- Route and page behavior belongs in web integration tests.
- SQL mapping belongs in opt-in data integration tests.
- Migration confidence belongs in content validation reports.
- Browser behavior belongs in a small Playwright end-to-end suite.
- End-user load cost belongs in the advisory frontend performance workflow, not in every PR.

## Pull Request Expectations

Every pull request should state:

- What was changed.
- Which test layers were run.
- Whether legacy database tests were run or intentionally skipped.
- Any remaining manual checks.

If a change touches legacy data access, canonical routes, content rendering, or publication rules, it should include tests or validation evidence for the affected behavior.

Pull requests should mention any meaningful coverage movement when the change adds risky logic or intentionally leaves a path untested.
