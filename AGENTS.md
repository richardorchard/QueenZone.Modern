# Agent Guide

This repository is the modern QueenZone rebuild. The first release is a read-only public archive backed by legacy content.

## Source Of Truth

- `README.md` gives the project overview and local development commands.
- `docs/architecture/testing-policy.md` defines the required testing layers.
- `docs/decisions/` contains accepted architectural decisions.
- `docs/backlog/migration-backlog.md` tracks migration work.

Keep durable workflow guidance in this file and keep user-facing setup guidance in `README.md`.

## Branch And Pull Request Policy

Do not push feature work directly to `main`.

Use a branch named after the agent doing the work, not a single shared prefix. The agent slug must match whoever is performing the task so parallel work from different tools stays distinguishable.

Branch format:

```text
{agent}-{task}
```

- `{agent}`: lowercase slug for the active agent or assistant (for example `grok`, `claude`, `codex`, `cursor`).
- `{task}`: short kebab-case description of the work (for example `news-pagination`, `seo-foundation`).

Examples:

```text
grok-news-pagination
claude-news-pagination
codex-gpt-5-news-routes
cursor-health-check
```

Use the prefix for the agent you are, not a default from an earlier session or another tool. Different agents working on the same area should use different branch names, such as `grok-news-pagination` and `claude-news-pagination`, rather than reusing one shared branch.

Before merging to `main`, open a pull request and get a review. The pull request should include:

- Summary of the change.
- Tests run.
- Whether real legacy database checks were run.
- Any skipped checks or known follow-up work.

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

## Migration Principles

- Preserve public content first.
- Keep the first release read-only.
- Do not port Web Forms architecture.
- Keep legacy SQL access inside `QueenZone.Data`.
- Treat the legacy database as an import source, not the permanent domain model.
- Prefer clean, stable, search-friendly canonical URLs over preserving legacy URL shapes.
- Never expose private, hidden, deleted, moderated, or credential-related data by default.
