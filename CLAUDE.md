# CLAUDE.md

This file is a lightweight Claude Code entry point for this repository.

`AGENTS.md` is the canonical, comprehensive instruction set. Read and follow `AGENTS.md` first for project architecture, branch policy, testing expectations, local secrets, and migration principles. Do not duplicate those details here; update `AGENTS.md` when a rule should apply to all AI coding agents.

## Claude Code Workflow

- Treat `AGENTS.md` as the source of truth. If this file and `AGENTS.md` disagree, `AGENTS.md` wins.
- Keep this file short and Claude-specific. Use it for provider-specific reminders, not repository facts that can drift.
- Use agent-prefixed branches such as `claude/news-pagination` (see `AGENTS.md`).
- Fill in the **Agent** line in `.github/pull_request_template.md` when opening a pull request.
- Before handing work back, run the default verification in `AGENTS.md` unless a smaller targeted test set is enough for the touched area.
- Real legacy database checks are opt-in. Report clearly whether they were run or skipped.

## Cross-Agent Handoffs

For multi-session or multi-provider work, use:

- `docs/agent-handoff-cheatsheet.md`

Keep handoffs concise: current branch and status, changed files, tests run, known failures, and the next concrete task. If a handoff reveals a durable repo rule, add it to `AGENTS.md` rather than copying it into provider-specific files.