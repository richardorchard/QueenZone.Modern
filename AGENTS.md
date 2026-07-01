# Agent Guide

This repository is the modern QueenZone rebuild. The project is archive-first: it should fully expose valuable legacy public content while keeping visitor-facing archive pages read-only. News is the first live editorial slice, so the architecture should also support newly approved news articles.

## Source Of Truth

- `README.md` gives the project overview and local development commands.
- `docs/architecture/testing-policy.md` defines the required testing layers.
- `docs/decisions/` contains accepted architectural decisions.
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

Default verification before a pull request:

```powershell
dotnet restore QueenZone.sln
dotnet build QueenZone.sln --configuration Release --no-restore
dotnet test QueenZone.sln --configuration Release --no-build
```

Use deterministic sample or fake data for normal unit and web integration tests. Real legacy database tests must be opt-in and clearly reported.

## Local Secrets

Do not commit secrets.

Local secrets belong in ignored files such as:

- `src/QueenZone.Web/appsettings.Local.json`
- `.env`

Commit only examples such as `.env.example`.

The deployed App Service runtime database setting is `ConnectionStrings__QueenZoneLegacy`. The current production route uses SQL authentication, stored in Azure App Service configuration, not in the repository.

The GitHub environment secret `QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING` is separate and is used by the deploy workflow for EF Core migrations. Updating the GitHub secret does not update the live App Service runtime connection string. When database credentials rotate, update both places as needed and restart the App Service before verifying production.

For production debugging (log stream, Azure CLI, Azure MCP tenant setup, and forum smoke checks), see `docs/agent-handoff-cheatsheet.md`.

For local SQL MCP access through Azure Data API Builder, see `docs/sql/data-api-builder-mcp.md`. Keep the MCP surface narrow and read-oriented by default.

## Migration Principles

- Preserve public content first.
- Keep the public archive read-only for visitors.
- Allow deliberately designed editorial workflows for new approved news articles.
- Do not port Web Forms architecture.
- Keep legacy SQL access inside `QueenZone.Data`.
- Treat the legacy database as an import source, not the permanent domain model.
- Prefer clean, stable, search-friendly canonical URLs over preserving legacy URL shapes.
- Never expose private, hidden, deleted, moderated, or credential-related data by default.

## Cursor Cloud specific instructions

Environment: .NET 10 SDK is preinstalled at `/usr/local/dotnet` and symlinked to `/usr/local/bin/dotnet` (already on `PATH`). The startup update script runs `dotnet restore QueenZone.sln` and `dotnet tool restore`; standard build/test/run commands live in `README.md` and above.

No database is required for local development. When `ConnectionStrings:QueenZoneLegacy` is empty (the default), the app uses in-memory/sample data, so `dotnet run --project src/QueenZone.Web/QueenZone.Web.csproj` starts with zero external services (defaults to `Development` at `http://localhost:5146`). `dotnet run` builds `Debug` by default; do not pass `--no-build` unless you have already built the `Debug` configuration (the `--configuration Release` builds live under `bin/Release`).

Exercising admin editorial routes locally without real Entra: admin routes require Microsoft Entra sign-in unless `AzureAd:ClientId` is blank, in which case a test-header auth fallback is active. `appsettings.json` ships a placeholder `ClientId`, so create a git-ignored `src/QueenZone.Web/appsettings.Local.json` that sets `AzureAd:ClientId` to `""` and lists an allowed admin email under `Admin:AllowedEmails`. Then authenticate admin requests by sending the `X-Test-User-Email: <allowed-email>` header. Admin POSTs need the `__RequestVerificationToken` antiforgery field, so fetch the form first and reuse its token plus cookie. The news article body is validated as plain text (HtmlSanitizer), so a body containing HTML tags is rejected with "Article body must be plain text."
