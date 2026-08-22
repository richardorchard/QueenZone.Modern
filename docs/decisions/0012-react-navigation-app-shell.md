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

Auth-gated vs public structure must match the website, not a mobile-only policy:

- Public: archive (news, biography, discography, timeline, Freddie Tribute),
  photography browse, forum browse, fan-performance listings, Contact, account
  sign-in.
- Members: private messages, forum compose/reply, photo submit, fan-performance
  audio, profile and settings.

## Decision

Use **React Navigation** (`@react-navigation/native`) in
`src/QueenZone.Mobile`:

- `createBottomTabNavigator` at the root.
- `createNativeStackNavigator` in each tab (Today, News, Photos, Forum,
  Messages, You).
- Signed-out tabs: Today, News, Photos, Forum, You.
- Signed-in tabs add **Messages** before You.
- Member-only screens also use an in-tree `MemberGate` so deep links cannot
  skip the boundary.
- Session state is a local development toggle until the Epic 0 token client is
  wired.

Do not introduce Expo Router for this shell. The project is already an Expo
development build; adding a second routing model would conflict with the
approved navigator shape.

Visual design-token porting remains [#792](https://github.com/richardorchard/QueenZone.Modern/issues/792).
This shell uses a temporary dark palette aligned with the spec.

## Consequences

Benefits:

- Feature epics attach screens to named stacks instead of inventing navigation.
- Native stack back gestures work on both platforms.
- Public vs member tabs cannot drift from the website header/content boundary.

Tradeoffs:

- Six tabs when signed in is one more than the five-tab visual spec. Messages
  is a first-class signed-in surface on the website, so it is a tab rather than
  a buried You-stack item.
- The development sign-in toggle is not the production PKCE flow.
