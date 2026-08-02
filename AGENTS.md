# Agent Guide

This repository is the modern QueenZone rebuild. The project is archive-first: it should fully expose valuable legacy public content while keeping visitor-facing archive pages read-only. News is the first live editorial slice, so the architecture should also support newly approved news articles.

## Source Of Truth

- `README.md` gives the project overview and local development commands.
- `docs/architecture/testing-policy.md` defines the required testing layers (including CI Web.Tests mixed sharding).
- `docs/decisions/` contains accepted architectural decisions.
- `docs/decisions/0006-hybrid-ef-core-admin-writes.md` is the Dapper vs EF access matrix and contributor rules for SQL in `QueenZone.Data`.
- `docs/architecture/blob-storage-ugc.md` is the UGC blob upload foundation (`QueenZone.Storage` / `IBlobUploadService`).
- `docs/decisions/0007-rich-text-editor-quill.md` is the shared Quill rich-text editor decision (partial + `/api/uploads/editor-image`).
- `docs/backlog/migration-backlog.md` tracks migration work.
- `docs/sql/data-api-builder-mcp.md` explains the local SQL MCP setup for read-only legacy database investigation.
- `docs/agent-bitwarden-secrets.md` is the multi-machine Bitwarden Secrets Manager (`bws`) setup for local agents (Windows vs macOS).

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
dotnet format QueenZone.sln --verify-no-changes
dotnet test QueenZone.sln --configuration Release --no-build
```

Use deterministic sample or fake data for normal unit and web integration tests. Real legacy database tests must be opt-in and clearly reported.

When changing EF `SqlQueryRaw` projections over legacy tables, check the real SQL Server column types or cast projections to the C# row model types explicitly. Many legacy IDs and counts are `smallint`, which SQL Server materializes as `System.Int16`; in-memory route tests will not catch `Int16`-to-`Int32` mapping failures. Prefer a deterministic SQL-shape test plus an opt-in read-only legacy DB probe for new public legacy read surfaces.

The read-only legacy probes now also run automatically every night against the real legacy database via `.github/workflows/nightly-legacy-checks.yml` on the self-hosted macOS runner — not a PR gate, just continuous signal. See `docs/architecture/testing-policy.md` ("Data Integration Tests").

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
| **Build** | `dotnet restore`, `dotnet build`, format verify (Release) | Yes |
| **Test (sharded)** | Mixed `QueenZone.Web.Tests` shards + small test projects (Release, Coverlet) | Yes |
| **Formatting** | `dotnet format QueenZone.sln --verify-no-changes` (matches root `.editorconfig`; CRLF via `.gitattributes`) | Yes |
| **Global line coverage** | At least **51%** across the union of deterministic suite reports | Yes |
| **Changed-line coverage** | At least **70%** of changed, coverable `.cs` lines in the PR diff vs `main` | Yes |
| **Smoke test** | Published app responds on `/health`, `/`, `/news` | Yes |
| **EF migrations (Azure SQL)** | When migration-related paths change: `has-pending-model-changes` + `database update` against the deploy SQL Server | Yes (job runs only for those PRs) |
| **Playwright e2e** | Self-hosted Windows runner; gates deploy when the runner is online | Yes (deploy needs a green e2e job) |

Coverage exclusions are configured in `coverlet.runsettings`. EF Core files under `**/Migrations/**/*.cs` are excluded from coverage metrics.

The changed-line gate compares `git diff origin/main...HEAD` for `*.cs` files. Large new modules (services, repositories, workers) usually need targeted unit or integration tests, often with fakes or SQLite/in-memory EF, or the gate will fail.

### CI Web.Tests sharding (agents)

CI parallelizes `QueenZone.Web.Tests` with **mixed** shards (light unit tests + `WebApplicationFactory` tests in every shard). Scripts: `scripts/Get-WebTestShardFilter.ps1`, `scripts/Invoke-WebTestsShard.ps1`. Full policy and anti-patterns: `docs/architecture/testing-policy.md` (section **CI test sharding**).

- Local default remains `dotnet test QueenZone.sln` (no filter).
- **Do not** split CI/jobs as unit-only vs WAF-only for speed — measured regression in [#442](https://github.com/richardorchard/QueenZone.Modern/issues/442).
- No shard manifest to maintain when adding tests; discovery is automatic from `*Tests` classes.

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
dotnet format QueenZone.sln --verify-no-changes
dotnet test QueenZone.sln --configuration Release --no-build --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory ./TestResults
powershell -File ./scripts/Test-CoverageGate.ps1 -Reports ./TestResults -GlobalLineThreshold 51 -ChangedLineThreshold 70 -BaseRef origin/main
```

On Linux or GitHub Actions, use `pwsh` instead of `powershell` for the last command.

If the gate reports uncovered changed lines, it prints up to 20 `path:line` entries. Add or extend tests until changed-line coverage is at least 70%.

Full detail, test-layer guidance, and coverage troubleshooting: `docs/architecture/testing-policy.md` (sections **Continuous Integration** and **Pre-pull request checklist**).

## Local Secrets

Do not commit secrets.

Local secrets belong in ignored files such as:

- `src/QueenZone.Web/appsettings.Local.json`
- `src/QueenZone.NewsAgent.Worker/appsettings.Local.json`
- `.env`

Commit only examples such as `.env.example`.

Bitwarden **Secrets Manager** (`bws` CLI) is the shared local secret store for development agents on Richard's machines. This is **not** the password-manager CLI (`bw`). Full multi-machine setup (Windows vs macOS, install paths, troubleshooting): [`docs/agent-bitwarden-secrets.md`](docs/agent-bitwarden-secrets.md).

Rules for agents:

- Use **`bws`**, never `bw login`, for QueenZone App Service–style secrets.
- Authenticate with user-scoped **`BWS_ACCESS_TOKEN`**; do not ask the user to paste the token into chat; never print tokens or secret values (key names and value lengths only).
- Machine account: **`windows-codex`** on Windows, **`mac-codex`** on Macs (separate tokens per host).
- Project: **`Queenzone Development`** (`1c16fd2d-4bfb-4eb7-8357-b49400233490`).
- **This Windows workstation:** `bws` lives at `%USERPROFILE%\bin\bws.exe` with `%USERPROFILE%\bin` on the User `Path`; token is User env `BWS_ACCESS_TOKEN`. Restart agent shells after Path changes.

```powershell
$env:BWS_ACCESS_TOKEN = [Environment]::GetEnvironmentVariable("BWS_ACCESS_TOKEN", "User")
bws secret list "1c16fd2d-4bfb-4eb7-8357-b49400233490"
```

App Service setting names are the canonical secret keys in Bitwarden, including `ConnectionStrings__QueenZoneLegacy`, `ConnectionStrings__BlobStorage`, `AzureAd__*`, `Authentication__*`, `Admin__AllowedEmails__*`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, and `OPENROUTER_API_KEY`. Azure App Service settings are a separate store: updating Bitwarden does not update Azure App Service. GitHub Actions now fetches its deploy secrets (`AZURE_WEBAPP_PUBLISH_PROFILE`, `QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING`) from this same Bitwarden project at workflow runtime via `bitwarden/sm-action`, so updating those Bitwarden secrets does flow into the next deploy run — see `docs/bitwarden-secrets.md`. When credentials rotate, update every authoritative target deliberately (Bitwarden, and Azure App Service settings if the runtime value also needs to change) and verify by name plus value length, not by printing values.

The deployed App Service runtime database setting is `ConnectionStrings__QueenZoneLegacy`. The current production route uses SQL authentication, stored in Azure App Service configuration, not in the repository.

`QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING` is fetched by the deploy workflow from Bitwarden (mapped from the same `ConnectionStrings__QueenZoneLegacy` secret) for EF Core migrations. Updating that Bitwarden secret does not update the live App Service runtime connection string, which is configured separately in Azure App Service settings. When database credentials rotate, update both places as needed and restart the App Service before verifying production.

For production debugging (log stream, Azure CLI, Azure MCP tenant setup, and forum smoke checks), see `docs/agent-handoff-cheatsheet.md`.

Admin Entra app registration, App Service `AzureAd__*` settings, and **client secret rotation** (renew by 2028-07-01): `docs/architecture/entra-admin-auth.md`.

Hosting scale/cost: production is **single-instance B1** with **no Redis**; see `docs/architecture/hosting-scale-and-cache.md` before proposing multi-instance or distributed cache work.

For local SQL MCP access through Azure Data API Builder, see `docs/sql/data-api-builder-mcp.md`. Keep the MCP surface narrow and read-oriented by default.

News agent worker and admin review queue: see `docs/architecture/news-agent.md`. OpenRouter key goes in `src/QueenZone.NewsAgent.Worker/appsettings.Local.json`. Manual OpenRouter smoke test: `scripts/Smoke-NewsAgent.bat`. Admin review UI: `/admin/news-discovery` (requires `Admin:AllowedEmails` from App Service or `appsettings.Local.json` — committed `appsettings.json` ships an empty allowlist; member OAuth at `/account/login` is unrelated).

Never log secrets (connection strings, client secrets, storage keys, OpenRouter keys) into issues, PR text, or application telemetry properties.

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
