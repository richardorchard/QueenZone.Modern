---
name: orchestrate-epic
description: Coordinate a GitHub epic or a numbered issue list until each item is done. Use for "work on #15 #16 #17", epic child fan-out, and mixed website plus QueenZone.Mobile work. Pin as a Custom Mode. Do not auto-apply to ordinary single-task chats.
disable-model-invocation: true
icon: git-branch
color: purple
---

# Orchestrate issues

You are the parent coordinator. Set the chat model to **Grok 4.6** at **high** (use **xhigh** only if the split is messy). Do not implement issues yourself. Your job is a queue, not a long coding session — that is how you keep a usable context window.

Subagents: `.cursor/agents/planner.md`, `implementer.md`, `verifier.md`, `reviewer.md`.

## Grok 4.6 effort

Leave each subagent's frontmatter model in place. Do not pass a Task `model` override unless a child is unusually hard (then you may raise implementer to `grok-4.6[effort=high]`).

| Role | Subagent | Model |
| --- | --- | --- |
| This chat | (parent) | `grok-4.6` high (xhigh only if the split is messy) |
| Order / split | `planner` | `grok-4.6[effort=high]` |
| One issue | `implementer` | `grok-4.6[effort=medium]` |
| Check one issue | `verifier` | `grok-4.6[effort=high]` |
| Review one issue | `reviewer` | `grok-4.6[effort=high]` |

## Queue

Work items are GitHub issues in `richardorchard/QueenZone.Modern`. Build the queue from:

- An explicit list (`work on 15, 16, 17` / `#757 #758`), or
- An epic's open children (e.g. #756).

Fetch each issue when its turn starts (title, body, labels, acceptance criteria). Do not paste every issue body into this chat up front.

**Scoreboard only in this chat** (one line per item): number, title, surface (`web` / `mobile` / `api` / `mixed`), status (`queued` / `blocked` / `in-progress` / `needs-retry` / `done` / `paused`), branch, PR if any. After each child returns, drop the child's logs; keep the scoreboard.

## Sequential only

Work **one issue at a time**, and **one subagent at a time** for that issue. Do not spawn a second implementer, verifier, or reviewer while another child is running. Do not overlap issues.

Share the parent checkout. **Do not isolate git worktrees or cloud VMs** for these children. Isolated checkouts re-run `.cursor/worktrees.json` (`dotnet restore QueenZone.sln`) and dominate wall-clock. `.cursor/worktrees.json` exists for true parallel agents, which this queue does not use.

If an item is blocked by an earlier unmerged issue, either stack onto that issue's branch (say so on the scoreboard) or **pause** that item. Do not start a sibling implementer to "fill time."

## Speed (expected vs waste)

A sequential issue taking many minutes is **expected**: fresh context per child, implementer coding, verifier re-running the touched tests, then a read-only review. That is the cost of not exhausting one window.

Do **not** add these — they are why earlier runs felt stuck:

- Isolated worktree / extra `dotnet restore` per child
- Parallel implementers
- Reviewer re-running the full test suite (verifier already did)
- Implementer or verifier running `dotnet test QueenZone.sln` when a named test project is enough
- Parent at xhigh for scoreboard-only turns
- Pasting every issue body into this chat up front

## Loop until done

1. If order is unclear, spawn **planner** once with the issue numbers (not full bodies) and ask for dependency order, shared files, and web vs mobile. Confirm with the user only when the split is ambiguous, will open many PRs, or needs credentials/devices.
2. Take the next unblocked item. One issue only.
3. Spawn **implementer** with a self-contained prompt (issue number, body or acceptance criteria, paths, tests, branch slug, "do not take sibling issues", "do not open a PR"). Wait until it returns.
4. Spawn **verifier** with that issue's acceptance criteria and branch. Do not mark done on the implementer's word. Wait until it returns.
5. If verifier fails: one retry implementer on the same issue, then **pause** (do not silently burn the rest of the queue).
6. Spawn **reviewer** on the same branch. Wait until it returns.
   - **Request changes**: one retry implementer, then re-run verifier then reviewer. If still blocking, **pause**.
   - **Approve** or **Nits only**: continue.
7. Open **one PR** for that issue (`gh pr create`, fill `.github/pull_request_template.md`). Fetch `origin/main` and merge it into the branch first. `Closes #<n>` for that issue; `Relates to #<epic>` when there is a parent. Include reviewer nits in Follow-up when the verdict was nits-only.
8. Pause for humans: Apple/Google/Bitwarden credentials, TestFlight/device checks, product questions. Then continue the queue.
9. Repeat until the queue is empty or paused. Report the scoreboard.

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
- Tests to run (named project, not the whole solution unless required)
- Agent slug (`cursor/` unless the user named another)
- "Do not expand scope. Do not take sibling issues."
- "Commit and push the branch. Do not open a pull request."
- "Do not create a git worktree. Do not restore packages unless they are missing."

## Each verifier prompt must include

- Issue number, branch, acceptance criteria, paths
- Tests to run (same named project as the implementer)
- "Do not change product code. Do not open a PR. Do not create a git worktree."

## Each reviewer prompt must include

- Issue number, branch, surface, paths, acceptance criteria
- "Review the diff against origin/main. Do not re-run the full test suite. Do not open a PR."

## QueenZone constraints

Tell implementers to read `AGENTS.md`. You still enforce: no push to `main`; `{agent}/{task}` branches; SQL only in `QueenZone.Data`; visitor/admin pages as Razor Pages; mobile stays out of `QueenZone.sln`. One PR per issue unless the user asked to batch. `Closes #<n>` for that issue; `Relates to #<epic>` when there is a parent.
