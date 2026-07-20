# Agent Guide

This repository is the modern QueenZone rebuild. The project is archive-first: it should fully expose valuable legacy public content while keeping visitor-facing archive pages read-only. News is the first live editorial slice, so the architecture should also support newly approved news articles.

## Source Of Truth

- `README.md` gives the project overview and local development commands.
- `docs/architecture/testing-policy.md` defines the required testing layers.
- `docs/decisions/` contains accepted architectural decisions.
- `docs/decisions/0006-hybrid-ef-core-admin-writes.md` is the Dapper vs EF access matrix and contributor rules for SQL in `QueenZone.Data`.
- `docs/architecture/blob-storage-ugc.md` is the UGC blob upload foundation (`QueenZone.Storage` / `IBlobUploadService`).
- `docs/decisions/0007-rich-text-editor-quill.md` is the shared Quill rich-text editor decision (partial + `/api/uploads/editor-image`).
- `docs/backlog/migration-backlog.md` tracks migration work.
- `docs/sql/data-api-builder-mcp.md` explains the local SQL MCP setup for read-only legacy database investigation.

Keep durable workflow guidance in this file and keep user-facing setup guidance in `README.md`.

## UI Architecture

`QueenZone.Web` uses ASP.NET Core Razor Pages for server-rendered pages. Public archive pages, news pages, and admin editorial screens should live under `src/QueenZone.Web/Pages` as `.cshtml` files with page models.

Do not build visitor-facing or admin pages by streaming inline HTML from minimal route handlers. Minimal endpoints are appropriate for small non-page responses such as `/health` or future JSON APIs.

## Branch And Pull Request Policy

Do not push feature work directly to `main`.

Use a branch named after the agent doing the work, not a single shared prefix. The agent slug must match whoever is performing the task so parallel work from different tools stays distinguishable.

Branch format:

```text
{agent}/{task}
```

- `{agent}`: lowercase slug for the active agent or assistant (for example `grok`, `claude`, `codex`, `composer`).
- `{task}`: short kebab-case description of the work (for example `news-pagination`, `seo-foundation`).

Examples:

| Agent / tool    | Prefix      | Example branch                | Auto label     |
| --------------- | ----------- | ----------------------------- | -------------- |
| Grok            | `grok/`     | `grok/news-pagination`        | `agent:grok`   |
| Claude Code     | `claude/`   | `claude/seo-foundation`       | `agent:claude` |
| Codex           | `codex/`    | `codex/legacy-news-dedup`       | `agent:codex`  |
| Cursor Composer | `composer/` | `composer/health-smoke-tests` | `agent:composer` |
| New tool        | `{name}/`   | `my-tool/forum-archive-review`| `agent:{name}` |

GitHub Actions applies the matching `agent:*` label from the branch prefix via `.github/workflows/agent-pr-label.yml`.

Use the prefix for the agent you are, not a default from an earlier session or another tool. Different agents working on the same area should use different branch names, such as `grok/news-pagination` and `claude/news-pagination`, rather than reusing one shared branch.

Before merging to `main`, open a pull request and fill in `.github/pull_request_template.md`. The pull request should include:

- Which agent authored the change.
- Summary of the change.
- Tests run.
- Whether real legacy database checks were run.
- Any skipped checks or known follow-up work.

For multi-session work, use `docs/agent-handoff-cheatsheet.md`.

## Testing Expectations

Follow `docs/architecture/testing-policy.md`.

### Default verification before a pull request

```powershell
dotnet restore QueenZone.sln
dotnet build QueenZone.sln --configuration Release --no-restore
dotnet test QueenZone.sln --configuration Release --no-build
```

Use deterministic sample or fake data for normal unit and web integration tests. Real legacy database tests must be opt-in and clearly reported.

When a change touches admin news writes or discovery-to-news promotion, prefer running the opt-in admin write probe before release or after deployment verification:

```powershell
$env:RUN_LEGACY_WRITE_PROBE = "true"
powershell -File .\scripts\Probe-AdminNewsLegacyWrites.ps1
```

Run it only when `ConnectionStrings__QueenZoneLegacy` points at a database you are willing to mutate. The probe creates, publishes, unpublishes, and deletes a uniquely named draft article to confirm the real SQL-backed admin workflow still works.

### Pull request CI gates (must pass before merge)

GitHub Actions workflow `.github/workflows/ci.yml` blocks merge when these fail:

| Check | Requirement | Blocks PR? |
| --- | --- | --- |
| **Build + test** | `dotnet restore`, `dotnet build`, `dotnet test` (Release) | Yes |
| **Global line coverage** | At least **51%** across the deterministic test suite | Yes |
| **Changed-line coverage** | At least **80%** of changed, coverable `.cs` lines in the PR diff vs `main` | Yes |
| **Smoke test** | Published app responds on `/health`, `/`, `/news` | Yes |
| **EF migrations (Azure SQL)** | When migration-related paths change: `has-pending-model-changes` + `database update` against the deploy SQL Server | Yes (job runs only for those PRs) |
| **Playwright e2e** | Runs on self-hosted Windows runner when available | No (`continue-on-error`) |

Coverage exclusions are configured in `coverlet.runsettings`. EF Core files under `**/Migrations/**/*.cs` are excluded from coverage metrics.

The changed-line gate compares `git diff origin/main...HEAD` for `*.cs` files. Large new modules (services, repositories, workers) usually need targeted unit or integration tests, often with fakes or SQLite/in-memory EF, or the gate will fail.

### EF migration PRs (required before merge)

If the PR touches any of:

- `src/QueenZone.Data/Migrations/`
- `src/QueenZone.Data/QueenZoneDbContext.cs`
- `src/QueenZone.Data/QueenZoneDbContextFactory.cs`
- `src/QueenZone.Data/Entities/`

then CI runs **EF migrations (Azure SQL)** against the same database as deploy (`QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING`). Unit/SQLite tests do **not** catch SQL Server batch-binding errors or Azure SQL timeouts.

Locally, before opening such a PR:

```powershell
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project src/QueenZone.Data/QueenZone.Data.csproj --startup-project src/QueenZone.Web/QueenZone.Web.csproj

$env:ConnectionStrings__QueenZoneLegacy = "<migration connection string>"
dotnet ef database update --project src/QueenZone.Data/QueenZone.Data.csproj --startup-project src/QueenZone.Web/QueenZone.Web.csproj
```

Rules of thumb for hand-written SQL migrations:

- Separate dependent DDL into separate `migrationBuilder.Sql(...)` calls (SQL Server batch binding).
- Prefer idempotent SQL; large indexes may need `suppressTransaction: true` and a higher command timeout.
- Keep the EF model snapshot in sync (or add a no-op sync migration).

### Pre-PR verification (recommended before opening the PR)

Run the same coverage gate locally so CI failures are caught early:

```powershell
git fetch origin main
dotnet restore QueenZone.sln
dotnet build QueenZone.sln --configuration Release --no-restore
dotnet test QueenZone.sln --configuration Release --no-build --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory ./TestResults
powershell -File ./scripts/Test-CoverageGate.ps1 -Reports ./TestResults -GlobalLineThreshold 51 -ChangedLineThreshold 80 -BaseRef origin/main
```

On Linux or GitHub Actions, use `pwsh` instead of `powershell` for the last command.

If the gate reports uncovered changed lines, it prints up to 20 `path:line` entries. Add or extend tests until changed-line coverage is at least 80%.

Full detail, test-layer guidance, and coverage troubleshooting: `docs/architecture/testing-policy.md` (sections **Continuous Integration** and **Pre-pull request checklist**).

## Local Secrets

Do not commit secrets.

Local secrets belong in ignored files such as:

- `src/QueenZone.Web/appsettings.Local.json`
- `src/QueenZone.NewsAgent.Worker/appsettings.Local.json`
- `.env`

Commit only examples such as `.env.example`.

The deployed App Service runtime database setting is `ConnectionStrings__QueenZoneLegacy`. The current production route uses SQL authentication, stored in Azure App Service configuration, not in the repository.

The GitHub environment secret `QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING` is separate and is used by the deploy workflow for EF Core migrations. Updating the GitHub secret does not update the live App Service runtime connection string. When database credentials rotate, update both places as needed and restart the App Service before verifying production.

For production debugging (log stream, Azure CLI, Azure MCP tenant setup, and forum smoke checks), see `docs/agent-handoff-cheatsheet.md`.

For local SQL MCP access through Azure Data API Builder, see `docs/sql/data-api-builder-mcp.md`. Keep the MCP surface narrow and read-oriented by default.

News agent worker and admin review queue: see `docs/architecture/news-agent.md`. OpenRouter key goes in `src/QueenZone.NewsAgent.Worker/appsettings.Local.json`. Manual OpenRouter smoke test: `scripts/Smoke-NewsAgent.bat`. Admin review UI: `/admin/news-discovery` (requires `Admin:AllowedEmails`; member OAuth at `/account/login` is unrelated).

## Media Serving

Two Cloudflare hostnames serve Azure Blob Storage content. They are **not interchangeable** — pick the right one for the content type.

| Hostname | Type | Can set response headers? | Use for |
| --- | --- | --- | --- |
| `cdn.queenzone.org` | Straight CDN proxy | No | Photos and images (`PhotoImageUrl`) |
| `cdn2.queenzone.org` | Cloudflare Worker proxy | Yes | Fan performance audio (`SongFileUrl`); legacy forum attachment redirect target |

`cdn2.queenzone.org` goes through a Worker, which allows `Content-Disposition` headers to be set on responses. This is required for fan performance audio so that the browser's native download button shows a consistent filename instead of "audio" (the last segment of the auth-gated endpoint path). Legacy forum attachments use the same Worker host after a member-auth gate (`/forum/attachment/legacy/{postId}`).

Do not switch `SongFileUrl` back to `cdn.queenzone.org`. Doing so silently breaks the download filename without causing any test failure. New forum uploads live in private `ugc-forum` and download via `/forum/attachment/{postId}/{attachmentId}` (member-only, app-streamed).

## Migration Principles

- Preserve public content first.
- Keep the public archive read-only for visitors.
- Allow deliberately designed editorial workflows for new approved news articles.
- Do not port Web Forms architecture.
- Keep all SQL Server access inside `QueenZone.Data` (no ad-hoc SQL in page models/tools). See ADR 0006 for the Dapper/EF matrix: new writes default to EF; complex legacy/projected reads may keep SQL/procs; target direction is EF Core as the single client library while retaining stored procedures for hot paths.
- Treat the legacy database as an import source and historical reference. Forum public reads use modern projected tables by default; other public content may keep reading legacy tables unless performance or safety problems appear.
- Prefer clean, stable, search-friendly canonical URLs over preserving legacy URL shapes.
- Never expose private, hidden, deleted, moderated, or credential-related data by default.

## Cursor Cloud specific instructions

Environment: .NET 10 SDK is preinstalled at `/usr/local/dotnet` and symlinked to `/usr/local/bin/dotnet` (already on `PATH`). The startup update script runs `dotnet restore QueenZone.sln` and `dotnet tool restore`; standard build/test/run commands live in `README.md` and above.

No database is required for local development. When `ConnectionStrings:QueenZoneLegacy` is empty (the default), the app uses in-memory/sample data, so `dotnet run --project src/QueenZone.Web/QueenZone.Web.csproj` starts with zero external services (defaults to `Development` at `http://localhost:5146`). `dotnet run` builds `Debug` by default; do not pass `--no-build` unless you have already built the `Debug` configuration (the `--configuration Release` builds live under `bin/Release`).

Exercising admin editorial routes locally without real Entra: admin routes require Microsoft Entra sign-in unless `AzureAd:ClientId` is blank, in which case a test-header auth fallback is active. `appsettings.json` ships a placeholder `ClientId`, so create a git-ignored `src/QueenZone.Web/appsettings.Local.json` that sets `AzureAd:ClientId` to `""` and lists an allowed admin email under `Admin:AllowedEmails`. Then authenticate admin requests by sending the `X-Test-User-Email: <allowed-email>` header. Admin POSTs need the `__RequestVerificationToken` antiforgery field, so fetch the form first and reuse its token plus cookie. The news article body is validated as plain text (HtmlSanitizer), so a body containing HTML tags is rejected with "Article body must be plain text."
