---
name: orchestrate-epic
description: Coordinate an epic by planning, fanning out child tasks to implementer subagents, and verifying each result. Invoke with /orchestrate-epic or pin as a Custom Mode (Alt+Enter on Windows, Option+Enter on Mac). Do not auto-apply to ordinary single-task chats.
disable-model-invocation: true
icon: git-branch
color: purple
---

# Orchestrate an epic

You are the parent coordinator in this chat. Set the chat model to **Grok 4.6** at **high** or **xhigh** effort. Do not implement child tasks yourself.

Subagent definitions: `.cursor/agents/planner.md`, `implementer.md`, `verifier.md`.

## Grok 4.6 effort

Leave each subagent's frontmatter model in place. Do not pass a Task `model` override unless a child is unusually hard (then you may raise implementer to `grok-4.6[effort=high]`).

| Role | Subagent | Model |
| --- | --- | --- |
| This chat | (parent) | `grok-4.6` high or xhigh |
| Plan / split | `planner` | `grok-4.6[effort=high]` |
| One child task | `implementer` | `grok-4.6[effort=medium]` |
| Check a child | `verifier` | `grok-4.6[effort=high]` |

## Protocol

1. If the epic has no file-level plan, spawn **planner** (read-only) with the epic/issue text and ask for independent vs blocked children.
2. Confirm the child list with the user when the split is ambiguous or will open more than one PR. Otherwise proceed.
3. Spawn **implementer** once per ready child. Independent children: multiple Task calls in one turn (parallel). Blocked children: wait. Overlapping files, or each child needs its own PR: ask for isolated worktrees / "own environment".
4. After each implementer returns, spawn **verifier** with the child's acceptance criteria, claimed files, and test commands. Do not mark the child done on the implementer's word.
5. Integrate, report remaining children, and stop when blocked rather than expanding scope.

## Each implementer prompt must include

- Issue/child id and title
- Paths it may touch
- Acceptance criteria
- Tests to run
- Agent slug for branches (`cursor/` unless the user named another)
- "Do not expand scope. Do not take sibling tasks."

## QueenZone constraints (do not restate AGENTS.md)

Tell implementers to read `AGENTS.md`. You still enforce: no push to `main`; `{agent}/{task}` branches; SQL only in `QueenZone.Data`; visitor/admin pages as Razor Pages.

If each child should be its own PR, do not pile implementers onto one branch — one isolated worktree (or Cloud Agent) per child.

## Isolation vs one session

- **This skill** — one epic, results return here, parent integrates.
- **Agents Window / Cloud Agents** — independent tickets, each with its own branch and PR.
