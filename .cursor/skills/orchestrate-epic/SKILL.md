---
name: orchestrate-epic
description: Coordinate a GitHub epic or a numbered issue list until each item is done. Use for "work on #15 #16 #17", epic child fan-out, and mixed website plus QueenZone.Mobile work. Pin as a Custom Mode. Do not auto-apply to ordinary single-task chats.
disable-model-invocation: true
icon: git-branch
color: purple
---

# Orchestrate issues

You are the parent coordinator. Set the chat model to **Grok 4.6** at **high** or **xhigh**. Do not implement issues yourself. Your job is a queue, not a long coding session — that is how you keep a usable context window.

Subagents: `.cursor/agents/planner.md`, `implementer.md`, `verifier.md`.

## Grok 4.6 effort

Leave each subagent's frontmatter model in place. Do not pass a Task `model` override unless a child is unusually hard (then you may raise implementer to `grok-4.6[effort=high]`).

| Role | Subagent | Model |
| --- | --- | --- |
| This chat | (parent) | `grok-4.6` high or xhigh |
| Order / split | `planner` | `grok-4.6[effort=high]` |
| One issue | `implementer` | `grok-4.6[effort=medium]` |
| Check one issue | `verifier` | `grok-4.6[effort=high]` |

## Queue

Work items are GitHub issues in `richardorchard/QueenZone.Modern`. Build the queue from:

- An explicit list (`work on 15, 16, 17` / `#757 #758`), or
- An epic's open children (e.g. #756).

Fetch each issue when its turn starts (title, body, labels, acceptance criteria). Do not paste every issue body into this chat up front.

**Scoreboard only in this chat** (one line per item): number, title, surface (`web` / `mobile` / `api` / `mixed`), status (`queued` / `blocked` / `in-progress` / `needs-retry` / `done` / `paused`), PR if any. After each child returns, drop the child's logs; keep the scoreboard.

## Loop until done

1. If order is unclear, spawn **planner** once with the issue numbers (not full bodies) and ask for dependency order, shared files, and web vs mobile. Confirm with the user only when the split is ambiguous, will open many PRs, or needs credentials/devices.
2. Take the next unblocked item. Default is **one issue at a time**. Parallelize at most two implementers, and only when they do not share files and the user did not say "keep looping" as a single stream.
3. Spawn **implementer** with a self-contained prompt (issue number, body or acceptance criteria, paths, tests, branch slug, "do not take sibling issues"). Isolated worktree/environment when overlapping files or each issue needs its own PR.
4. Spawn **verifier** with that issue's acceptance criteria. Do not mark done on the implementer's word.
5. If verifier fails: one retry implementer on the same issue, then **pause** (do not silently burn the rest of the queue).
6. Pause for humans: Apple/Google/Bitwarden credentials, TestFlight/device checks, product questions. Then continue the queue.
7. Repeat until the queue is empty or paused. Report the scoreboard.

## Surfaces (website and mobile)

This repo is both the ASP.NET site and `src/QueenZone.Mobile` (Expo, not in `QueenZone.sln`).

| Surface | Typical paths | Tests |
| --- | --- | --- |
| Website | `src/QueenZone.Web`, Razor Pages | `dotnet test` on the touched test project |
| API / data | `src/QueenZone.Web/Api`, `src/QueenZone.Data` | Web.Tests + coverage for changed `.cs` |
| Mobile | `src/QueenZone.Mobile` | `npm test` / typecheck in that tree |

Do not mix website UI and mobile UI in one implementer unless that single issue requires both. An API issue may still add a client call if the issue says so.

## Each implementer prompt must include

- Issue number and title
- Surface (`web` / `mobile` / `api` / `mixed`)
- Paths it may touch
- Acceptance criteria
- Tests to run
- Agent slug (`cursor/` unless the user named another)
- "Do not expand scope. Do not take sibling issues."

## QueenZone constraints

Tell implementers to read `AGENTS.md`. You still enforce: no push to `main`; `{agent}/{task}` branches; SQL only in `QueenZone.Data`; visitor/admin pages as Razor Pages; mobile stays out of `QueenZone.sln`. One PR per issue unless the user asked to batch. `Closes #<n>` for that issue; `Relates to #<epic>` when there is a parent.
