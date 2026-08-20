# Mobile app epics and user stories

Speculative backlog for a native iPhone (later Android) app, following on from [`mobile-app-feasibility.md`](../architecture/mobile-app-feasibility.md). This is planning scope only — nothing here is accepted work. Per [`migration-backlog.md`](migration-backlog.md)'s rule, open a GitHub epic/issue before starting any of it, and re-check the PWA's real usage first, since the feasibility doc's recommendation was PWA-first and native work is only justified if that sees traction.

Ordering below is dependency order, not priority: Epic 0 is a hard prerequisite for every other epic, since none of the app's core content is currently available as an API.

Tech choice: **React Native** for both iOS and Android — see [ADR 0009](../decisions/0009-react-native-for-mobile-app.md).

## Epic 0 — Mobile API & token auth foundation

The site is 100% server-rendered Razor Pages with cookie session auth today ([`mobile-app-feasibility.md`](../architecture/mobile-app-feasibility.md)). Nothing else in this backlog can start until there's a JSON API and a token auth flow a native app can use.

- As a mobile client, I want a token-based sign-in flow (OAuth2 PKCE against the existing Google/Microsoft/Discord/GitHub providers, or a new mobile-specific flow) so members don't need an embedded browser session to stay signed in.
- As a mobile client, I want short-lived access tokens plus a refresh flow so the app can stay signed in across sessions without storing a long-lived credential insecurely.
- As a backend maintainer, I want a versioned JSON API surface (`/api/v1/...`) alongside the existing Razor Pages, so the website and the app can evolve independently.
- As a backend maintainer, I want the admin `Admin:AllowedEmails` gating and member-vs-admin scheme separation preserved in the new API, so the mobile API doesn't accidentally widen admin access.
- As a security reviewer, I want rate limiting and abuse protection on the new auth endpoints equivalent to what the web login flow has today, so the mobile surface doesn't become the weak link.

## Epic 1 — Read the archive (News, Biography, Discography, Timeline, Freddie Tribute)

The lowest-risk, highest-content-value epic: mostly read-only archive material with no write paths to secure.

- As a visitor, I want to browse News, Biography, Discography, Timeline, and the Freddie Tribute in the app so I get the same core content as the website without a browser.
- As a returning reader, I want previously viewed articles to stay available offline (mirroring the PWA's cache-first static assets + cached-navigation approach already shipped), so spotty connectivity doesn't block reading.
- As a visitor, I want news articles to support the same rich content (images, embedded media) the web version renders, so nothing is lost in translation to a native view.
- As a subscriber to specific content areas, I want to control whether new News items surface a notification, so the app isn't noisy by default.

## Epic 2 — Forum

- As a member, I want to browse forum categories and topics in the app, matching the structure of `Pages/Forum/`.
- As a member, I want to read a topic's full post thread, including attachments, without leaving the app.
- As a member, I want to create a new topic and reply to existing ones from the app.
- As a member, I want to vote in and see results for forum polls (mirroring `Endpoints/ForumPollEndpoints.cs`) natively.
- As a member, I want a push notification when someone replies to a topic I'm following, so I don't have to keep checking.

## Epic 3 — Private messaging

Builds on the mobile-usability work already done for this feature on the web (PR #714) — the app should reach parity, not reinvent it.

- As a member, I want to see my inbox with unread counts, matching the badge behavior added for the mobile web view.
- As a member, I want to read a full conversation thread and send replies from the app.
- As a member, I want to compose a new message to another member from the app.
- As a member, I want a push notification when I receive a new private message, since this is the single feature most likely to justify a native app over the PWA per the feasibility assessment.
- As a member, I want the "archived" and "sending blocked/privacy-disabled" states from the web version to behave identically in the app, so behavior doesn't diverge by platform.

## Epic 4 — Photo galleries & submissions

- As a visitor, I want to browse the public photo galleries in the app with smooth, native-feeling scrolling and image loading.
- As a member, I want to submit a photo (and article, if in scope) from the app, including picking straight from my camera or camera roll — this is one of the few points of real native-device advantage over the PWA.
- As a member, I want to see the status of my submission (pending, approved, rejected) matching the existing `Admin/PhotoSubmissions` review workflow.
- As a backend maintainer, I want the mobile upload path to reuse the existing per-member daily upload quota (`MemberUploadQuotaService`) so mobile doesn't get a separate, unenforced limit.

## Epic 5 — Fan performances (audio)

- As a signed-in member, I want to browse and stream fan performance recordings in the app, matching the member-gating already enforced by `FanPerformanceEndpoints.cs`.
- As a listener, I want playback to continue in the background and show up in the iOS lock-screen/Control Center media controls, which is not achievable in a browser tab — another genuine native-only win.
- As a listener, I want to see track duration before playing (parity with the web feature shipped in PR #710).

## Epic 6 — Member account & profile

- As a member, I want to view and edit my profile (matching `Pages/Members/`) from the app.
- As a member, I want to manage my avatar, including uploading a new one from my camera or photo library.
- As a member, I want to sign out, and to request account data deletion, matching the existing privacy/data-deletion pages under `Pages/Account/`.
- As a visitor, I want to submit a Help request from the app, matching the public `Help/` form and its admin inbox review (PR #711).

## Epic 7 — Push notifications

Cross-cutting infrastructure epic that most of the above stories depend on. If pursued via the PWA-first path instead, this becomes Web Push rather than APNs — see the feasibility doc.

- As a backend maintainer, I want a device-token/subscription registration endpoint and per-member storage, so we know where to deliver notifications.
- As a member, I want granular notification preferences (forum replies, private messages, news) so I control what interrupts me.
- As a backend maintainer, I want notification delivery hooked into the existing forum-post, message-send, and news-publish code paths without duplicating that business logic.
- As an on-call maintainer, I want notification delivery failures logged and monitored so silent failures don't go unnoticed on a single-instance, low-budget deployment.

## Epic 8 — Offline, performance & sync

- As a member on a poor connection, I want previously loaded forum threads, messages, and articles available read-only offline.
- As a member, I want actions taken offline (e.g. a drafted forum reply) queued and sent automatically once connectivity returns, rather than silently failing.
- As a backend maintainer, I want the mobile API's response payloads and caching headers sized for the existing single-B1-instance hosting budget, so mobile traffic doesn't force a hosting-tier upgrade (see [`hosting-scale-and-cache.md`](../architecture/hosting-scale-and-cache.md)).

## Epic 9 — iOS App Store release

- As the product owner, I want the app to include at least one capability genuinely absent from mobile Safari (push notifications and/or lock-screen audio controls) before submission, to clear Apple's Minimum Functionality guideline (4.2) per the feasibility assessment.
- As the product owner, I want an Apple Developer Program account, App Store listing (screenshots, description, privacy policy linkage to the existing `Account` data-deletion/privacy pages), and TestFlight beta pass completed before public submission.
- As a compliance reviewer, I want the App Store's required "Sign in with Apple" consideration addressed if the app offers other third-party logins (Google/Microsoft/Discord/GitHub), per Apple's guideline 4.8.
- As the product owner, I want crash reporting and basic usage analytics in the shipped app so post-launch issues surface quickly on a solo-maintained project.

## Epic 10 — Android port

Deliberately last: the user's stated priority is iPhone first, Android later, and most of Epics 0–8 are platform-agnostic once the API/auth foundation exists. Per [ADR 0009](../decisions/0009-react-native-for-mobile-app.md), React Native was chosen specifically so this epic is largely a build-and-release exercise rather than a second app.

- As an Android user, I want feature parity with the iOS app for all the epics above, delivered from the same React Native codebase rather than a rewrite.
- As the product owner, I want any iOS-only native modules used in Epics 4/5/7 (camera, background audio, push) audited for Android equivalents early, so Epic 10 isn't blocked by a module with no Android support.
- As the product owner, I want a Google Play Store listing and compliance pass (Play's own minimum-functionality and data-safety requirements) completed before submission, distinct from Apple's requirements.
