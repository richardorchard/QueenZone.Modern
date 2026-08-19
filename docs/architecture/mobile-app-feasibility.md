# Mobile app feasibility

Assessment from 2026-08-19 of what a native iPhone (later Android) app would require, and whether QueenZone would clear App Store review. Written before any mobile-app work started; the PWA groundwork this doc recommends was implemented in the same pass (see [Outcome](#outcome-implemented-2026-08-19)).

## What the site looks like today

- **No API layer.** Every page (news, forum, messages, galleries) is server-rendered ASP.NET Core Razor Pages HTML. The only JSON/non-HTML endpoints under `src/QueenZone.Web/Endpoints/` are narrow — RSS, file/audio streaming, poll votes — nothing a native app could consume as structured data for core content.
- **Cookie-based auth**, not tokens. Members sign in via Google/Microsoft/Discord/GitHub OAuth into a session cookie (`src/QueenZone.Web/Auth/QueenZoneAuthServiceCollectionExtensions.cs`). A native app can't reuse this session directly; it would need either a fragile embedded-browser session or a new token auth scheme built specifically for mobile.
- **No real-time/push infrastructure.** No SignalR, no WebSockets, no web push. Unread-message badges are computed per page load, not pushed.
- **Hobby-scale hosting.** Single B1 Azure App Service instance, no CDN, no autoscaling (see [`hosting-scale-and-cache.md`](hosting-scale-and-cache.md)). No monetization anywhere in the repo.
- **Mobile-web investment already exists**: a responsive nav drawer, a fix making private messaging usable on small screens (PR #714), and an admin-only PWA (`wwwroot/admin-manifest.webmanifest` + `_PwaHead.cshtml`, formerly `_AdminPwaHead.cshtml`) that let `/Admin` install to an iOS home screen. The public/member site had no manifest, no service worker, no installability before this assessment.

## The App Store viability question

Apple's App Store Review Guideline 4.2 ("Minimum Functionality") is the operative risk: Apple rejects apps that are just a repackaged website with no functionality beyond what mobile Safari already provides. QueenZone has real content depth (forum, news, galleries, private messaging, member accounts) but, as of this assessment, nothing that differentiates a native app from visiting the site in Safari — no offline mode, no push notifications, no device integration (e.g. camera-based photo submission). Submitted as-is (a bare WebView wrapper), rejection risk is high. Clearing the bar needs at least one genuine native capability layered on — most realistically push notifications for new forum replies/private messages, the one thing mobile web fundamentally can't do as well as native (or a PWA with web push).

## Recommendation: PWA first, native only if it earns its keep

Given this is a solo, hobby-scale project with no monetization, a full native rewrite now is disproportionate: it would require inventing a token-based auth API, a JSON API for every screen, native UI built from scratch, a $99/year Apple Developer Program membership, and ongoing dual-platform maintenance.

A **Progressive Web App** extension of the existing site gets most of the practical value for near-zero incremental backend work, reusing the existing cookie session as-is and requiring no App Store submission or review.

If push notifications (or another genuine native hook, e.g. camera-integrated photo submission via the existing `Submit/` flow) are added later, the lowest-effort path into the App Store is a Capacitor shell around the PWA rather than a from-scratch SwiftUI rewrite — it still needs the Apple Developer Program and a push subscription backend, but avoids building a full REST API and native UI layer.

## Outcome (implemented 2026-08-19)

Extended the existing admin-only PWA pattern to the whole site:

- `src/QueenZone.Web/Pages/Shared/_AdminPwaHead.cshtml` renamed to `_PwaHead.cshtml`; it now emits `admin-manifest.webmanifest` under `/Admin` and a new public `manifest.webmanifest` everywhere else, both with the same `#111111` theme color used site-wide.
- `wwwroot/manifest.webmanifest` — public manifest, `start_url`/`scope` `/`, `display: standalone`, reusing the existing `apple-touch-icon.png` / `favicon-512.png` icons.
- `wwwroot/sw.js` — cache-first for static assets (`/css`, `/js`, `/design-system`, favicons), network-first with cache fallback for page navigations, so previously visited pages stay reachable offline. `wwwroot/js/pwa-register.js` registers it on page load.

Not done (deliberately deferred, per the recommendation above): web push, and anything native/App Store-facing. Those are separate follow-on efforts, only worth pursuing if this PWA sees real usage.

Verify installability with Lighthouse's PWA checklist via the existing tooling: `scripts/Measure-FrontendPerformance.ps1 -FormFactor mobile`.
