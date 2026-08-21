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
npm run typecheck
npm test
```

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
| `staging` | `https://queenzone-dev.azurewebsites.net` |
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
The You tab shows the active `appEnv` and resolved origin for a quick check.

Call sites should use `apiV1Url('/content/news')` (or `getAppConfig().apiBaseUrl`)
rather than hard-coding hosts.

## Navigation shell

React Navigation provides the app shell ([ADR 0012](../../docs/decisions/0012-react-navigation-app-shell.md)).

Signed-out tabs: **Today · News · Photos · Forum · You**.

Signed-in tabs add **Messages** (member-only, matching the website header).

Placeholder screens exist for Epics 1–6. You → **Sign in (development)** toggles
the local session until the Epic 0 token client is wired. Member-only routes
also render a sign-in gate so they stay closed while signed out.

Rebuild the development client after this native dependency set changes
(`react-native-screens`, `react-native-safe-area-context`, `react-native-svg`):

```powershell
npx expo prebuild --clean
npx expo run:android
```

## CI build pipeline

`mobile-js` (typecheck + unit tests), `mobile-android`, and `mobile-ios`
(native compile) in [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)
run whenever `src/QueenZone.Mobile/` changes, or on demand via
`workflow_dispatch`. `mobile-android` builds an unsigned debug APK on a
GitHub-hosted Linux runner; `mobile-ios` builds an unsigned Simulator `.app`
(zipped) on a GitHub-hosted macOS runner — no Apple account or signing
credentials are used. Both jobs upload their build as a workflow artifact
(`mobile-android-<run-id>` / `mobile-ios-<run-id>`), downloadable from the
run's summary page for one day.

Production signing, TestFlight, and store submission remain separate release
concerns (ADR 0011). The installable Android build below uses a test-only key.

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
engines not to index it. The publishing design is recorded in
[ADR 0013](../../docs/decisions/0013-static-web-app-mobile-test-distribution.md).

The separate `publish-mobile-test-build.yml` workflow publishes after mobile
changes merge to `main`, and can also be run manually. A signed,
device-installable iOS build remains follow-up work under #808.
