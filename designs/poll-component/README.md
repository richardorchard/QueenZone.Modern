# Handoff: Poll Component

## Overview
A community poll UI for Queenzone.org — used to poll fans in forum discussion threads and as standalone/featured polls (e.g. on the homepage or a "Popular Discussions" module). Covers desktop, mobile web, and the native mobile app, in light and dark mode.

## About the Design Files
The files in this bundle (`Poll Card.dc.html`, `Poll Component.dc.html`) are **design references built in HTML** — prototypes showing intended look, states and interaction, not production code to copy directly. Recreate these designs in the target codebase's existing environment (React, Vue, native, etc.) using its established component patterns — do not ship the HTML as-is. If the codebase already has the Queenzone design system implemented (Button, Badge, Tag components, color/type/spacing tokens), build the poll component against those existing primitives rather than reinventing them.

## Fidelity
**High-fidelity.** Colors, typography, spacing and states are final per the Queenzone Design System. Recreate pixel-for-pixel using the codebase's existing design-system components/tokens.

## Component structure
Two pieces:
- **Poll Card** — the reusable poll itself (`Poll Card.dc.html`). Recreate as a single reusable component (e.g. `<PollCard>`), driven by the props below.
- **Poll Component** page — shows Poll Card composed into three contexts (desktop, mobile web, mobile app) with a light/dark toggle. This is a showcase, not a screen to ship — use it to see every state/context combination at once.

### Poll Card props
| Prop | Type | Notes |
|---|---|---|
| `context` | `'standalone' \| 'thread'` | `standalone` = featured poll (shows a "Community Poll" eyebrow label). `thread` = embedded under a forum post (shows author avatar, name, post meta). |
| `choiceType` | `'single' \| 'multiple'` | Single = radio buttons, voting on click. Multiple = checkboxes + an explicit "Cast your vote" button. |
| `phase` | `'before' \| 'after' \| 'closed'` | `before` = options only, interactive. `after` = results shown (just voted). `closed` = results shown, no interaction, "Closed" badge. |
| `dark` | `boolean` | Renders the dark/rich-black variant used on the archive's dark section bands. |
| `question` | `string` | The poll question. |
| `options` | `{label: string, pct: number}[]` | Option labels and their result percentage (percentages are the source of truth for the results bars/labels; not derived from live vote counts client-side in this prototype). |
| `totalVotes` | `number` | Total vote count, shown in the meta row. |
| `closesText` / `closedText` | `string` | e.g. "Closes in 4 days" / "Closed 12 Aug 2026". |
| `authorName` / `authorMeta` | `string` | Thread context only — poster's name and post meta line (e.g. "Posted in Live Aid 1985 · 41 replies"). |
| `userVoteIndices` | `number[]` | Which option index/indices the viewing user picked — used to highlight "your vote" in `after`/`closed` states. |

## Layout
**Card**: white (or black in dark mode) surface, 1px hairline border, `radius-md` (4px) corners, `shadow-card` (light mode only — no shadow in dark mode), 24px padding, full width of its container.

**Header row** (flex, space-between, ~14px bottom margin):
- Thread context: 34px circular avatar (initial letter) + name (13px/600) + meta (11px/500, muted) stacked.
- Standalone context: "COMMUNITY POLL" eyebrow — Cinzel, 11px, uppercase, wide tracking, Royal Blue (light) / Antique Gold (dark).
- Closed phase: a "Closed" badge, right-aligned — Badge component, `tone="neutral" variant="outline"` in light mode, `tone="special" variant="solid"` in dark mode (outline-on-charcoal is illegible on a black card — use the solid gold badge instead).

**Question**: Cormorant Garamond, `--type-h3` (30px/600), 6px bottom margin.

**Helper text** (multiple + before only): "Select all that apply", italic, 13px, muted.

**Option rows**: flex column, 10px gap. Each row: 13px/14px padding, 1px border, `radius-sm` (3px), flex row, 12px gap:
- Indicator: 18px — circle for single choice, `radius-xs` square for multiple. Border `grey-400` (light) / `rgba(255,255,255,0.35)` (dark) when unselected; filled Royal Blue with a white checkmark (10px, 3.5px stroke) when selected/voted.
- Label: 15px/400 Inter, 600 weight + full opacity when it's the user's vote.
- Before voting: plain bordered row, pointer cursor, `--qz-blue-tint` background wash when checked (multiple, pre-submit).
- After voting/closed: an absolutely-positioned result bar behind the label fills from the left to `pct`%, animated (`width var(--dur-slow) var(--ease-out)`) — `--qz-blue-tint` (or `rgba(36,74,143,0.32)` in dark) for the user's own pick, a neutral grey/white-alpha tint for other options. Percentage shown right-aligned, bold, tabular numbers; Royal Blue (light) or white (dark) for the user's pick.
- Voted row border becomes Royal Blue.

**Vote button** (multiple + before only): Queenzone `Button`, `variant="cta"`, `size="sm"`, full width, disabled until ≥1 option is checked. Label: "Cast your vote".

**Meta row**: top border (hairline), 16px top margin, 14px top padding, flex space-between. Left: "`{n}` votes" (formatted with thousands separator). Right: `closesText` (before), "You voted" (after), or `closedText` (closed). Uppercase, 11.5px/500, 0.06em tracking, muted color.

## Interactions & Behavior
- **Single choice, before**: clicking any option immediately casts the vote and transitions the card to the results view (`after`) — no separate submit step.
- **Multiple choice, before**: clicking toggles a checkbox (no vote cast yet); the "Cast your vote" button enables once ≥1 is checked; clicking it transitions to `after`.
- **After/closed**: rows are inert (no cursor/hover change); percentages and the result bars are static per the `options[].pct`/`userVoteIndices` data passed in — this prototype does not recompute vote math live.
- Transitions use the design system's motion tokens: border/background `var(--dur-fast)` (180ms), result bar width `var(--dur-slow)` (620ms), both `var(--ease-out)`.

## Dark mode
A light/dark toggle exists on the showcase page purely for review — in the real product, `dark` should be driven by which section band the poll sits in (the archive's existing dark-band sections), not a user-facing setting. Dark-mode deltas: card surface → `--qz-black`; borders → `--border-on-dark`; body text → `--text-on-dark` / `--text-on-dark-muted`; card shadow removed; eyebrow label → `--qz-gold` (dark-band accent per brand); "Closed" badge switches to `tone="special" variant="solid"`.

## Design Tokens
All values come from the existing Queenzone Design System tokens — no new colors/type/spacing were introduced:
- Colors: `--qz-white`, `--qz-black`, `--qz-blue` / `--qz-blue-tint`, `--qz-gold`, `--qz-grey-*`, `--text-primary/secondary/muted/on-dark/on-dark-muted`, `--border-default/on-dark`.
- Type: `--font-display` (Cormorant Garamond) for the question, `--font-titling` (Cinzel) for eyebrows/badges, `--font-body` (Inter) for everything else. `--type-h3` for the question.
- Radius: `--radius-sm` (rows/buttons), `--radius-md` (card), `--radius-xs` (checkbox), `--radius-pill` (mobile-web address bar).
- Shadow: `--shadow-card` (light card), `--shadow-lift` (mobile-web frame).
- Motion: `--dur-fast`/`--dur-slow`, `--ease-out`.

## Assets
No custom icons or images — one inline checkmark glyph (line icon, matches the system's Lucide-style ~1.5px stroke convention) and, in the mobile-web mock, a simple back-chevron and lock glyph for the browser chrome (mock-up only, not part of the shippable component).

## Files
- `Poll Card.dc.html` — the reusable poll component (all states/props described above).
- `Poll Component.dc.html` — showcase page: desktop (2 cards side by side), mobile web (browser-chrome phone frames), mobile app (iOS device frames), plus the light/dark toggle. Reference this to see every context × choice-type × phase × theme combination.
- `screenshots/dark-mode-full.png` — dark mode, page top (header + toggle + start of desktop section).
- `screenshots/dark-mobile-sections.png` — dark mode, mobile-web section (both poll cards in the browser-chrome frames).
- `screenshots/dark-app-section.png` — dark mode, mobile-app section (both poll cards in the iOS device frames).
