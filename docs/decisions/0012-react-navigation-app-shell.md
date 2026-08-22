# ADR 0012: React Navigation App Shell

## Status

Accepted.

## Context

[Issue #791](https://github.com/richardorchard/QueenZone.Modern/issues/791)
needs a navigation library and a base app shell mapped to Epics 1–6, with
signed-out and signed-in surfaces separated from the start. The approved mobile
design spec (`design/Queenzone mobile app design/handoff/QUEENZONE_APP_SPEC.md`)
already assumes React Navigation: a bottom tab navigator at the root and a
native stack per tab.

The v2 handoff (22 August 2026) restructured the tab bar so it mirrors the
website nav rather than inventing an app-only IA. Profile is a rare destination
and does not deserve a permanent fifth of the tab bar. Private messages remain
member-only, but they sit behind the Home masthead avatar rather than adding a
sixth tab.

Auth-gated vs public *content* still matches the website, not a mobile-only
policy:

- Public: archive (news, biography, discography, timeline, Freddie Tribute),
  photography browse, forum browse, fan-performance listings, Contact, account
  sign-in, profile (signed-out variant).
- Members: private messages, forum compose/reply, photo submit, fan-performance
  audio, settings and saved library.

## Decision

Use **React Navigation** (`@react-navigation/native`) in
`src/QueenZone.Mobile`:

- `createBottomTabNavigator` at the root.
- `createNativeStackNavigator` in each tab.
- **Five tabs, both platforms, signed-in and signed-out:**
  Home · News · Photography · Archive · Forum.
- Tab glyphs are Lucide outline `house`, `newspaper`, `camera`, `archive`,
  `message-square`.
- Profile, settings, search, saved lists, and private messages live on
  `HomeStack`, reached from the Home masthead avatar / search icon.
- New archive sections become rows on the Archive hub, never a sixth tab.
- Member-only screens use an in-tree `MemberGate` so deep links cannot skip
  the boundary.
- Session state is a local development toggle until the Epic 0 token client is
  wired.

Do not introduce Expo Router for this shell. The project is already an Expo
development build; adding a second routing model would conflict with the
approved navigator shape.

Chrome differences (status bar, nav bar, tab bar, sheets, FAB, press feedback)
come from `theme.chrome[Platform.OS]` — never from `Platform.select` inside a
screen file.

## Consequences

Benefits:

- Feature epics attach screens to named stacks instead of inventing navigation.
- Native stack back gestures work on both platforms.
- The tab bar matches the website and the approved visual spec.
- Public vs member *screens* cannot drift from the website content boundary.

Tradeoffs:

- Messages is no longer a first-class tab (it was in the v1 shell). Members
  open it from Profile. That is a deliberate v2 design decision, not an
  omission of Epic 3.
- The development sign-in toggle is not the production PKCE flow.
