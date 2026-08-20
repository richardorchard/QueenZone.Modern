# ADR 0009: React Native For The Native Mobile App

## Status

Accepted.

## Context

[`mobile-app-feasibility.md`](../architecture/mobile-app-feasibility.md) assessed native mobile app options against the existing site: server-rendered ASP.NET Core Razor Pages, cookie-session auth, no JSON API, no push/real-time infrastructure, single hobby-scale B1 App Service. That assessment recommended a PWA first (shipped: `wwwroot/manifest.webmanifest`, `wwwroot/sw.js`) and, if native ever became worthwhile, suggested a Capacitor wrapper around the PWA as the lowest-effort path into the App Store.

The product owner has since chosen to invest real effort in a native-feeling app rather than the lowest-effort wrapper, and prefers **React Native** over Capacitor or a fully separate native (Swift/Kotlin) codebase.

Candidates considered:

- **Capacitor** (wraps the existing PWA) — least effort, reuses all Razor-rendered HTML/CSS/JS, but stays a web view under the hood and gives the least native-feeling UI.
- **React Native** — genuine native UI components and navigation, one JS/TS codebase shared across iOS and Android, large ecosystem for camera/push/audio-background-playback modules needed by [Epics 4, 5, and 7](../backlog/mobile-app-epics.md). Requires building a real JSON API and token auth (Epic 0) since there is no Razor UI to reuse.
- **Flutter** — comparable native-feel and cross-platform reach to React Native, but a separate language/toolchain (Dart) with less overlap with the team's existing ASP.NET Core/JS skill set.
- **Separate native Swift + Kotlin codebases** — best possible per-platform UI/performance, but doubles ongoing maintenance for a solo maintainer; ruled out as disproportionate for this project's scale.

## Decision

Build the native mobile app in **React Native**, targeting iOS first and Android second, per [`mobile-app-epics.md`](../backlog/mobile-app-epics.md).

Use the supported local toolchain in the [mobile development environment guide](../mobile-development-environment.md) when scaffolding or building the client.

- Epic 0 (JSON API + token auth) is a hard prerequisite and must land before any screen work starts.
- The existing PWA stays live and maintained independently; React Native is additive, not a replacement for the web experience.
- Native-module choices (push notifications, camera/photo picker, background audio for fan performances) should prefer well-maintained community or Expo modules over hand-rolled native bridges where one exists, to keep the app maintainable by a solo developer.

## Consequences

Benefits:

- One codebase covers both iOS and Android (Epics 9 and 10), rather than two separate native apps.
- Native UI components and navigation give a materially better feel than a Capacitor/WebView wrapper.
- Large ecosystem of maintained modules for the native capabilities this app actually needs (push, camera, background audio) that this project's feasibility assessment identified as the real justification for going native at all.

Tradeoffs:

- Requires building and securing an entirely new JSON API + token auth surface (Epic 0) that does not exist today — the single biggest cost of this decision versus the Capacitor path.
- Two runtimes to reason about in production (server-rendered web + React Native client) instead of one.
- React Native's native-module ecosystem still occasionally requires native (Swift/Kotlin) code for edge cases; the solo maintainer should budget for this rather than assume pure JS/TS suffices throughout.
