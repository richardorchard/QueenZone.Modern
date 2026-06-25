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

Good targets:

- Homepage loads.
- News archive navigation works.
- News detail pages are crawlable.
- News archive and detail pages are crawlable in a browser.
- Mobile viewport smoke checks.
- Basic accessibility smoke checks.

Keep end-to-end tests small. They should prove critical user journeys and browser behavior, not duplicate all route integration tests.

## Continuous Integration

Every pull request must run:

```powershell
dotnet restore QueenZone.sln
dotnet build QueenZone.sln --configuration Release --no-restore
dotnet test QueenZone.sln --configuration Release --no-build
```

CI should also collect coverage from the deterministic test suite and publish an HTML/Cobertura report artifact. The coverage report is expected to help reviewers spot untested risk, but the project does not enforce a global percentage threshold yet because early migration work will mix framework glue, Razor rendering, and legacy-data boundaries.

For local coverage reports:

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

## Pull Request Expectations

Every pull request should state:

- What was changed.
- Which test layers were run.
- Whether legacy database tests were run or intentionally skipped.
- Any remaining manual checks.

If a change touches legacy data access, canonical routes, content rendering, or publication rules, it should include tests or validation evidence for the affected behavior.

Pull requests should mention any meaningful coverage movement when the change adds risky logic or intentionally leaves a path untested.
