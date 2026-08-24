# QueenZone.Mobile

Expo development-build client for QueenZone. Android and iOS are equal
supported platforms. This project is not part of `QueenZone.sln`.

Decisions: [ADR 0009](../../docs/decisions/0009-react-native-for-mobile-app.md),
[ADR 0011](../../docs/decisions/0011-mobile-project-location-and-build-tooling.md),
[ADR 0012](../../docs/decisions/0012-react-navigation-app-shell.md).
Host toolchain: [mobile development environment](../../docs/mobile-development-environment.md).

## Pinned versions

| Tool | Version |
| --- | --- |
| Expo SDK | **57** (`expo@~57.0.15`) |
| React Native | **0.86.2** |
| Node.js | **24 LTS** (`>=24 <25`) |
| npm | Bundled with Node.js |
| JDK | **17** (Eclipse Temurin) |
| Android `compileSdk` / `targetSdk` | **36** (from Expo SDK 57) |
| Android SDK Build-Tools | **36.0.0** |
| Xcode | **26.4+** (SDK 57 requirement) |
| Application id | `org.queenzone.mobile` |

Expo Go is **not** a supported build or test environment. Use a development
build (`expo-dev-client`) compiled with `npx expo run:android` or
`npx expo run:ios`.

EAS is **not** required. Do not add an Expo account, EAS project, or `eas.json`
for this initial project.

## Prerequisites

Install the shared toolchain from
[`docs/mobile-development-environment.md`](../../docs/mobile-development-environment.md)
before the first native build.

On both platforms:

- Node.js 24 LTS
- Git
- JDK 17, with `JAVA_HOME` pointing at that JDK
- Android Studio with Android SDK Platform 36, Build-Tools 36.0.0, platform-tools, and an API 36 Google APIs emulator image

`ANDROID_HOME` (or `ANDROID_SDK_ROOT`) must point at the SDK:

- Windows: `%LOCALAPPDATA%\Android\Sdk`
- macOS: `$HOME/Library/Android/sdk`

iOS additionally requires macOS, Xcode 26.4 or newer, the iOS Simulator runtime,
and CocoaPods. `npx expo run:ios` installs pods during prebuild.

## Clean checkout

From the repository root:

```powershell
cd src/QueenZone.Mobile
node --version   # v24.x
npm ci
npm run preflight
```

`npm run preflight` runs typecheck, the unit tests, and the lockfile-pinned
Expo Doctor check (`npm run doctor`). Doctor must pass all checks after a
clean `npm ci`. A required CI/publish Doctor gate is tracked in #870; run
the same `npm run doctor` command locally before opening a mobile PR.

SDK 57 always uses React Native's New Architecture, so `app.json` does not
set `newArchEnabled` (the field is no longer in the config schema). Splash
is configured through the `expo-splash-screen` plugin, not a top-level
`splash` object.

Generate native projects from committed Expo config (Continuous Native
Generation). Do not commit the resulting `android/` or `ios/` directories.

### Android (Windows or macOS)

```powershell
npx expo run:android
```

This generates `android/`, compiles a development-client APK, installs it on a
running emulator or device, and starts the Metro bundler. Rebuild the native
app only after changing native dependencies, `app.json`, or the Expo SDK:

```powershell
npx expo prebuild --clean
npx expo run:android
```

JavaScript-only changes use:

```powershell
npx expo start --dev-client
```

### iOS (macOS only)

```bash
npx expo run:ios
```

This generates `ios/`, installs CocoaPods, compiles a Simulator development
client, and starts Metro. The same `--clean` prebuild rule as Android applies
when native configuration changes.

## Theme

Design tokens live in `src/theme/` (#792). Palette hex values match
`wwwroot/design-system/tokens/colors.css`; type, space, radius, and motion
follow the same CSS foundation plus the mobile handoff at
`design/Queenzone mobile app design/handoff/` (`STYLE_GUIDE.md`, `theme.ts`).

The app is **dark-first** (`#111111` page, Antique Gold `#B89A4A` as
`accentPrimary`). Import via `useTheme()` — do not hard-code colours already
named in the theme.

**Fonts:** Cormorant Garamond, Inter, and Cinzel load at startup through
`useQueenzoneFonts()` (`@expo-google-fonts/*` TTFs, same families as the web
WOFF2s). Family names live in `theme.fonts`. After adding `expo-font` /
`expo-splash-screen`, rebuild the development client:

```powershell
npx expo prebuild --clean
npx expo run:android
```

## API base URL (environments)

Per-environment API origins live in `src/config/` (#793). `app.config.ts`
writes `extra.appEnv` and `extra.apiBaseUrl`; runtime code reads them via
`getAppConfig()` / `apiV1Url()`.

| `EXPO_PUBLIC_APP_ENV` | Default API origin |
| --- | --- |
| `development` (default) | `http://localhost:5146` (local `QueenZone.Web`) |
| `staging` | `https://www.queenzone.org` |
| `production` | `https://www.queenzone.org` |

Override the origin for any environment without code changes:

```powershell
# Point a local build at the HTTPS launch profile
$env:EXPO_PUBLIC_API_BASE_URL = "https://localhost:7162"
npx expo start --dev-client
```

```powershell
# Staging defaults
$env:EXPO_PUBLIC_APP_ENV = "staging"
npx expo start --dev-client
```

Copy `.env.example` to `.env` (git-ignored) for a sticky local override. Restart
Metro after changing env vars.

Android emulators rewrite `localhost` / `127.0.0.1` to `10.0.2.2` automatically.
Physical devices need your machine's LAN IP in `EXPO_PUBLIC_API_BASE_URL`.
The Profile screen (Home masthead avatar) shows the active `appEnv` and resolved origin for a quick check.

Call sites should use `apiV1Url('/content/news')` (or `getAppConfig().apiBaseUrl`)
rather than hard-coding hosts.

## Crash and error monitoring

[Sentry](https://sentry.io) (`@sentry/react-native`) reports JS exceptions and
native crashes. Configured in the `self-0tb` org, project `queenzone-mobile`
(#855). It stays a no-op — `initSentry()` in `src/config/sentry.ts` returns
immediately — until `EXPO_PUBLIC_SENTRY_DSN` is set, so builds and local dev
work fine without it (e.g. a fresh clone before copying `.env.example`).

| Variable | Where | Purpose |
| --- | --- | --- |
| `EXPO_PUBLIC_SENTRY_DSN` | `.env` locally; `vars.SENTRY_DSN` repo variable in CI | Enables reporting. DSNs are not secret. |
| `SENTRY_ORG` / `SENTRY_PROJECT` | CI repo `vars` | `self-0tb` / `queenzone-mobile` — target org/project for source map and dSYM upload at build time. |
| `SENTRY_AUTH_TOKEN` | Bitwarden `Queenzone Development` project (Android) / `secrets.SENTRY_AUTH_TOKEN` repo secret (iOS) | Lets `sentry-cli` upload symbols during the native build. Never committed — read directly from the build environment, not from Expo config. |
| `SENTRY_DISABLE_AUTO_UPLOAD` | CI unsigned mobile jobs (`ci.yml`); optional locally | Skips source-map / dSYM upload when org/token are unset. Required for simulator/debug CI builds once the Sentry Expo plugin is registered — otherwise Xcode fails with "organization ID or slug is required". |

Unsigned CI Android/iOS jobs set `SENTRY_DISABLE_AUTO_UPLOAD=true`. Publish workflows
(`publish-mobile-test-build.yml`, `publish-ios-testflight.yml`) set org/project/token
and upload symbols. For a local native build without Sentry credentials, export the
same disable flag (or set org/project/token as in `.env.example`).

To rotate the auth token or point at a different Sentry org/project: create an
org token (Sentry → Settings → Auth Tokens → Create New Organization Token;
the default scopes — Source Map Upload, Release Creation, Code Mappings — are
sufficient), then update the `SENTRY_AUTH_TOKEN` secret in Bitwarden and
GitHub, and the `SENTRY_DSN`/`SENTRY_ORG`/`SENTRY_PROJECT` repo variables if
the org/project changed.

## Navigation shell

React Navigation provides the app shell ([ADR 0012](../../docs/decisions/0012-react-navigation-app-shell.md)).
The v2 design handoff lives at
[`design/Queenzone mobile app design/handoff/`](../../design/Queenzone%20mobile%20app%20design/handoff/).

Five tabs, signed-in and signed-out: **Home · News · Photography · Archive · Forum**.
Profile, settings, and private messages sit behind the Home masthead avatar —
never a sixth tab. New archive sections become rows on the Archive hub.

Home, Archive hub, Photography, Forum, Search, and Profile follow the approved
screen contracts in `QUEENZONE_APP_SPEC.md`. News and photography browse live
content from `/api/v1` (photo image URIs are `cdn.queenzone.org`, never App Service).
Signed-in members can submit a photo from the camera or photo library
(`Photography` → Submit a photo). That posts multipart to
`POST /api/v1/member/photo-submissions` — the same `PhotoSubmissionService` /
`ugc-photos` review queue as website `/submit/photo`. Camera and library access
use `expo-image-picker` (already required for member avatars).
Fan performances list from `/api/v1/content/fan-performances`; streaming uses
`GET /api/v1/content/fan-performances/{id}/audio` with the member Bearer token
(same private `songfiles` blob as the website). Background playback and lock-screen
controls come from `expo-audio`.
Home → avatar → **Sign in** toggles the local session until the Epic 0 token
client is wired. Member-only routes also render a sign-in gate so they stay
closed while signed out.

Rebuild the development client after this native dependency set changes
(`react-native-screens`, `react-native-safe-area-context`, `react-native-svg`,
`expo-image`, `expo-image-picker`, `expo-linear-gradient`, `expo-audio`):

```powershell
npx expo prebuild --clean
npx expo run:android
```

## CI build pipeline

`mobile-js` (typecheck + unit tests), `mobile-android`, and `mobile-ios`
(native compile) in [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)
run whenever `src/QueenZone.Mobile/` changes, or on demand via
`workflow_dispatch`. Local preflight is `npm run preflight` (typecheck, unit
tests, and lockfile-pinned `npm run doctor`). Making Doctor a required
`mobile-js` / publish check is #870. `mobile-android` builds an unsigned debug APK on a
GitHub-hosted Linux runner; `mobile-ios` builds an unsigned Simulator `.app`
(zipped) on a GitHub-hosted macOS runner — no Apple account or signing
credentials are used. Both jobs upload their build as a workflow artifact
(`mobile-android-<run-id>` / `mobile-ios-<run-id>`), downloadable from the
run's summary page for one day.

The unsigned jobs are PR compile checks only. A separate manual workflow,
[`publish-ios-testflight.yml`](../../.github/workflows/publish-ios-testflight.yml),
archives and signs the iOS app on the dedicated self-hosted Mac runner and
uploads it to TestFlight. The installable Android build below uses a test-only
key.

## Install the latest iOS TestFlight build

The one-time Apple setup for `org.queenzone.mobile` consists of:

- Apple Developer team `X28Z75P69M`;
- an Apple Distribution certificate;
- the `QueenZone App Store` App Store Connect provisioning profile;
- the QueenZone App Store Connect record (Apple ID `6803889011`); and
- a Developer-role App Store Connect API key dedicated to GitHub uploads.

Run **Publish iOS to TestFlight** from the repository's **Actions** tab and
select `main`. The workflow intentionally rejects other branches and targets
the self-hosted Mac runner through `[self-hosted, macOS, ARM64, ios-signing]`.
The runner service does not load an interactive shell profile, so the workflow
puts Homebrew (`/opt/homebrew/bin` or `/usr/local/bin`) on `PATH`, installs
CocoaPods if `pod` is missing, runs `expo prebuild --no-install` with
`IOS_BUILD_NUMBER` set to the workflow run number (so `CFBundleVersion` is unique
for App Store Connect), records the UTC build time and source revision for the
in-app build stamp, then runs `pod install` before archiving. Expo also writes
`ITSAppUsesNonExemptEncryption=false` because the app uses only exempt platform
HTTPS; this prevents each TestFlight build pausing for the same export-compliance
questionnaire. Expo's own CocoaPods auto-install is skipped
because a missing CLI is only a warning and otherwise continues without an
`.xcworkspace`. It then imports signing material into a temporary Keychain,
produces and verifies a signed `.ipa`, retains that IPA as a seven-day
workflow artifact, uploads it to App Store Connect, and deletes the temporary
Keychain and provisioning profile even when a step fails.

The exported IPA verification checks the build number, exempt-encryption
declaration, timestamp, and source revision before upload. The app shows the
version, native build number, localised build date/time, and short revision at
the bottom of the **Profile** screen (Home masthead avatar), using the same
subdued build-stamp treatment as the website.

Install Apple's TestFlight app on the iPhone and accept the QueenZone internal
tester invitation. After Apple finishes processing an uploaded build, install
or update QueenZone from TestFlight; the phone does not need to connect to this
Mac. Each TestFlight build remains testable for 90 days.

The workflow uses these encrypted GitHub Actions secrets (names only; never
commit their values):

| Secret | Purpose |
| --- | --- |
| `IOS_DISTRIBUTION_CERTIFICATE_BASE64` | Base64-encoded password-protected Apple Distribution `.p12` |
| `IOS_DISTRIBUTION_CERTIFICATE_PASSWORD` | Password for the distribution `.p12` |
| `IOS_PROVISIONING_PROFILE_BASE64` | Base64-encoded `QueenZone App Store` `.mobileprovision` |
| `APP_STORE_CONNECT_KEY_ID` | App Store Connect API key identifier |
| `APP_STORE_CONNECT_ISSUER_ID` | App Store Connect API issuer identifier |
| `APP_STORE_CONNECT_PRIVATE_KEY` | One-time-downloaded App Store Connect `.p8` private key |

Rotate the distribution certificate/profile before their shared expiry and
replace the corresponding secrets together. Revoke and replace the API key if
its private key is ever exposed. Signing material must never be copied into the
repository, workflow artifacts, logs, or issue/PR text.

## Install the latest Android test build

Open [dev.queenzone.org](https://dev.queenzone.org) on an Android phone and tap
**Download latest APK**. The page shows the build date and time in Western
Australian time, file size, and source revision. No GitHub login or computer is
required.

Android may ask the first time whether the browser can install unknown apps.
Allow that browser, return to the download, and confirm the installation. Later
builds install as updates because the package identifier and test signing key
stay stable.

If a locally built or earlier CI debug version is already installed, Android may
report a package conflict because that copy used a different signing key.
Uninstall it once, then install the downloaded test build. Builds downloaded
from this page will update one another normally.

The APK is a pre-release build connected to the staging API. It and its download
page are public to anyone who knows the URL, although the page asks search
engines not to index it. The page is hosted by Azure Static Web Apps; the APK is
served from a separate, throwaway-build-only Azure Storage account so it cannot
affect production media or UGC. The publishing design is recorded in
[ADR 0013](../../docs/decisions/0013-static-web-app-mobile-test-distribution.md).

The separate `publish-mobile-test-build.yml` workflow publishes Android after
mobile changes merge to `main`, and can also be run manually. iOS remains an
explicit manual TestFlight release so signing and upload never run merely
because a pull request was opened or merged.
