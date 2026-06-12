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

Use a branch named after the agent or model doing the work. For this agent, use:

```text
codex-gpt-5
```

If multiple parallel efforts are needed, append a short task suffix while keeping the agent/model prefix, for example:

```text
codex-gpt-5-news-routes
```

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
