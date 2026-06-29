# Agent Handoff Cheat Sheet

Quick reference for starting fresh sessions on long-running QueenZone migration work.

## When To Start A Fresh Session

**Do it when:**

- Moving to a new major GitHub issue or content area
- After merging a pull request
- Starting route, canonical URL, or public rendering work
- After a long exploration or legacy SQL investigation phase
- The agent is losing precision on hard constraints

**You can usually continue when:**

- Still deep in one focused sub-task on the same issue
- Only a small, well-understood tweak is needed

**Pro tip:** Leave a `TODO` or `NOTE` comment in the code marking the exact resumption point.

## Agent Branch Prefixes

Agent branches use `{prefix}/{description}`. GitHub Actions adds an `agent:{prefix}` label to the pull request.

| Agent / tool    | Prefix      | Example branch                 |
| --------------- | ----------- | ------------------------------ |
| Grok            | `grok/`     | `grok/news-pagination`         |
| Claude Code     | `claude/`   | `claude/seo-foundation`        |
| Codex           | `codex/`    | `codex/legacy-news-dedup`      |
| Cursor Composer | `composer/` | `composer/health-smoke-tests`  |
| New tool        | `{name}/`   | `my-tool/forum-archive-review` |

Also fill in the **Agent** line when opening the pull request (see `.github/pull_request_template.md`).

## Category Quick Rules

| Category | Must remember |
| -------- | ------------- |
| **Routes / canonical URLs** | `/news` is canonical page 1; use `/news/page/{n}` for later pages; wrong slugs redirect to canonical detail URLs; do not preserve legacy Web Forms URL shapes by default |
| **Legacy SQL / data** | Keep SQL in `QueenZone.Data`; treat `DISPLAY = 1` as the public visibility gate; deduplicate by stable article ID before paging; legacy DB tests are opt-in |
| **SQL MCP** | Local agents can expose a narrow read-only DAB MCP server for legacy DB inspection; see `docs/sql/data-api-builder-mcp.md`; do not expose private/user/mail/IP/moderation data |
| **Public rendering** | Never expose hidden, deleted, moderated, draft, or private records in public output |
| **SEO / crawlability** | Unique page titles, canonical links, and crawlable HTML matter; avoid duplicate canonical pages |
| **Testing** | Pure logic in unit tests; route behavior in web integration tests with sample/fake data by default; report whether legacy DB checks ran |
| **Secrets / deploy** | No secrets in git; App Service runtime uses `ConnectionStrings__QueenZoneLegacy` with SQL authentication; GitHub migrations use the separate `QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING` secret; say what was tested before merge |

## Most-Used Minimal Template

```markdown
Resume #[ISSUE_NUMBER] on `[agent]/[short-desc]`.

Status: [concise state]. Next: [exact next step or TODO location].

Key rules:

- Use the correct agent branch prefix (`grok/`, `claude/`, `codex/`, etc.).
- Reference #[ISSUE_NUMBER] in commits when useful.
- Fill in the PR **Agent** line when opening the pull request.
- Run the default dotnet build/test gate before handoff.
- Report whether legacy database checks were run or skipped.

Read the TODO in [specific file] and the GitHub issue first.
```

## Production Debugging (Azure)

App Service: `queenzone-dev` in resource group `Queenzone-RG` (Australia East).

Public hostnames: `queenzone.org`, `www.queenzone.org`, `queenzone-dev.azurewebsites.net`.

### Azure CLI (preferred for agents)

Requires `az login` as a user with access to subscription **Base Thinking**.

Live log stream:

```powershell
az webapp log tail --name queenzone-dev --resource-group Queenzone-RG
```

Download recent logs:

```powershell
az webapp log download --name queenzone-dev --resource-group Queenzone-RG --log-file appservice-logs.zip
```

Quick health checks:

```powershell
az webapp show --name queenzone-dev --resource-group Queenzone-RG --query "{hostNames:hostNames, state:state}"
Invoke-WebRequest -Uri https://queenzone.org/health -UseBasicParsing | Select-Object StatusCode
```

Application logging should be enabled at Information level on the filesystem. If the stream is quiet, hit the failing route to generate entries.

### Azure MCP

The Azure MCP server must authenticate to tenant `c9f094fd-23bf-4a35-a406-bcaacd7e1a8e`. If tools return `InvalidAuthenticationTokenTenant`, run:

```powershell
az login --tenant c9f094fd-23bf-4a35-a406-bcaacd7e1a8e
```

Then restart the Azure MCP server in Cursor. `az account show` succeeding does not guarantee MCP is on the same tenant.

Useful MCP tools after auth is fixed:

- `subscription_list`
- `appservice` → `appservice_webapp_get` for site details
- `appservice` → `appservice_webapp_diagnostic_list` / `appservice_webapp_diagnostic_diagnose` for health probes

When MCP auth fails, fall back to Azure CLI commands above.

### Forum production smoke checks

After a forum deploy, verify:

- `GET https://queenzone.org/forum` → 200
- `GET https://queenzone.org/forum/1/queen-serious-discussion` → 200

In the log stream, `SqlException: Execution Timeout` in `LegacyForumRepository` usually means a full-table `COUNT(*)` on `Q_FORUM_TOPIC_T` or other heavy ad-hoc SQL. The modern read path should use denormalised `Q_FORUM_T.Q_FORUM_POST_COUNT`, `dbUser.Q_FORUM_TOPIC_THREAD_COUNT_V`, and `Q_FORUM_VIEW_PAGE_SP` instead. See `docs/legacy/table-map.md`.

### Modern forum import status

`docs/sql/004-modern-forum-batched-import.sql` creates the first modern forum archive read-model tables and resumable import procedures:

- `ModernForumCategory`
- `ModernForumThread`
- `ModernForumPost`
- `ImportCheckpoint`

It was applied to `queenzone-db` on 2026-06-29 while the database was on the 5 DTU / 2 GB Basic tier. The initial import used a too-narrow `Q_FORUM_TOPIC_PARENT_ID = 0` thread rule and was reset. The corrected import treats `TOPIC_STARTER = 1 OR Q_FORUM_TOPIC_PARENT_ID = 0` as legacy threads and carries attachment fields.

The corrected live run imported 88,679 threads, but the post import hit Azure SQL error `40544` at 570,000 imported posts because the database reached its 2 GB size quota before the full 1,164,816-row `Q_FORUM_TOPIC_T` corpus could fit alongside the legacy schema. The partial `ModernForum*` tables were reset, and `DBCC SHRINKFILE (data_0, 1800)` reduced the data file back to 1.8 GB allocated. As of that cleanup, the modern tables are not populated in production; the procedures remain available for a future run on a larger or separate database.

The public site still reads forum pages through `LegacyForumRepository`; switching public reads to the modern tables is follow-up work and should include parity checks against the legacy routes before production rollout.

## Quick Links

- Primary agent instructions: [AGENTS.md](../AGENTS.md)
- Claude entry point: [CLAUDE.md](../CLAUDE.md)
- Testing policy: [docs/architecture/testing-policy.md](architecture/testing-policy.md)
- SQL MCP setup: [docs/sql/data-api-builder-mcp.md](sql/data-api-builder-mcp.md)
- Migration backlog: [docs/backlog/migration-backlog.md](backlog/migration-backlog.md)
