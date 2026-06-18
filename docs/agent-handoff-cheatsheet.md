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

## Quick Links

- Primary agent instructions: [AGENTS.md](../AGENTS.md)
- Claude entry point: [CLAUDE.md](../CLAUDE.md)
- Testing policy: [docs/architecture/testing-policy.md](architecture/testing-policy.md)
- Migration backlog: [docs/backlog/migration-backlog.md](backlog/migration-backlog.md)
