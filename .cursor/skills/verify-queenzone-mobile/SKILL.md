---
name: verify-queenzone-mobile
description: Drive the QueenZone Expo mobile client the way a member does — launch an isolated Testing contract host, install a Debug build baked at that origin, and exercise Maestro flows. Use when proving home, news, photos, search, or forum behavior after a QueenZone.Mobile UI or navigation change.
---

# Verify QueenZone.Mobile

`src/QueenZone.Mobile` is the Expo development-build client (`org.queenzone.mobile`). This skill launches a disposable `Testing` contract host (in-memory sample data, `QUEENZONE_MOBILE_CONTRACT_HOST=1`) and drives the real Android or iOS app through Maestro. Read `features/README.md` before a run, then the matching feature file.

Do not use `verify-queenzone` (the Razor website) to prove mobile screens. Do not use Expo Go. Do not point this host at Azure SQL, a live site, or OAuth.

## Launch

Start the contract host only through the helper. Default bind is `http://127.0.0.1:5098` — the same port `scripts/run-mobile-device-smoke.sh` uses — so this run never collides with local Development (`5146`), Playwright E2E (`5099`), or `verify-queenzone` (`5199`).

From the repository root:

```powershell
pwsh -File .cursor/skills/verify-queenzone-mobile/scripts/control-queenzone-mobile.ps1 launch
```

Ready when:

- `GET /health` returns JSON `{"status":"ok"}`
- The helper has written `.cursor/skills/verify-queenzone-mobile/.run/host.json` with `"environment": "Testing"`
- The helper prints the base URL

The host uses `ASPNETCORE_ENVIRONMENT=Testing`, `QUEENZONE_MOBILE_CONTRACT_HOST=1`, and `--no-launch-profile`. Connection-string env vars are cleared for that process. The Android smoke APK must be baked at `http://10.0.2.2:5098`; iOS Simulator at `http://127.0.0.1:5098`. A Debug binary aimed at production cannot talk to this host.

Print the URL later with:

```powershell
pwsh -File .cursor/skills/verify-queenzone-mobile/scripts/control-queenzone-mobile.ps1 url
```

The emulator or Simulator is shared hardware. Use a device that is already booted; do not start a second emulator to "isolate" this run. Never drive a Metro/`expo start` session the user already has open, and never attach the app to `https://www.queenzone.org`. If port 5098 is owned by a process this skill did not start, pass `-Port` or stop.

Two contract hosts can run on different ports. Two copies of the app on one emulator cannot; refuse rather than uninstall someone else's session.

Teardown is Cleanup below. Leave the host up for the whole drive, then clean up. Do not kill the emulator.

## Doctor

Run this first whenever anything looks off, and once after launch before driving:

```powershell
pwsh -File .cursor/skills/verify-queenzone-mobile/scripts/control-queenzone-mobile.ps1 doctor
```

Doctor is read-only. It must report:

- A state file from this skill, with a live PID that owns the recorded port.
- `GET {url}/health` is 200 and body contains `"status":"ok"`.
- `.run/host.json` exists, `environment` is `Testing`, and `member.accessToken` is present (print only the token length).
- `GET {url}/api/v1/content/news/1003` is 200 and `title` is `QueenZone modernisation begins`. That is the payload `NewsStoryScreen` fetches.

Before a Maestro drive, also confirm:

- `adb devices` lists a device (Android) or a Simulator is booted (iOS, macOS only).
- `maestro` is on `PATH`.
- A Debug APK/app baked at this host's origin is installed, or you will rebuild with `scripts/run-mobile-device-smoke.sh`.

If doctor fails, stop. Do not fall back to another URL or to Expo Go.

## Drive

Maestro is the harness. Prefer `testID` values from `src/QueenZone.Mobile/src/test/testIds.ts` over visible copy. Run one flow file, not the whole suite, unless the change spans tabs.

From the repository root, with the helper host still up and `SMOKE_AUTH_URL` only for the authenticated flow:

```powershell
pwsh -File .cursor/skills/verify-queenzone-mobile/scripts/control-queenzone-mobile.ps1 drive -Flow news
```

That runs `src/QueenZone.Mobile/maestro/flows/04-news-story.yaml` against `org.queenzone.mobile`. Other `-Flow` values: `launch`, `tabs`, `home`, `news`, `photos`, `search`, `forum`, `profile`, `auth`.

Full CI-shaped suite (rebuilds and reinstalls the Debug binary):

```bash
./scripts/run-mobile-device-smoke.sh --platform android
```

On this Windows workstation iOS is not runnable. Treat iOS execution as CI-only.

Seeded Testing titles that must stay stable:

| Surface | Maestro id | Visible title |
| --- | --- | --- |
| Home | `home-screen`, `home-hero` | Hero opens story `QueenZone modernisation begins` |
| News list | `news-screen`, `news-story-1003` | Same title |
| News story | `news-story-screen` | `QueenZone modernisation begins` |
| Photos | `photos-screen`, `photo-category-brian-may`, `photo-item-101` | Brian May / photo 101 |
| Search | `search-input`, `search-result-news-1003` | Query `modernisation` |
| Forum | `forum-board-1`, `forum-thread-1002` | `Ranking every studio album` |
| Signed-out profile | `profile-signed-out` | `Join the archive` |
| Signed-in profile | `profile-signed-in` | `Contract Member` (Debug `queenzone://smoke-auth` only) |

`queenzone://smoke-auth` exists only in `__DEV__` Debug builds. It is not a production bypass. Never print `SMOKE_AUTH_URL` or the access token.

## Evidence

Write proof under `.cursor/skills/verify-queenzone-mobile/artifacts/<feature-id>/`. Cleanup must not delete this directory.

Proof standards:

- Drive the installed app through Maestro (or the same `testID` taps). Do not treat `npm test`, OpenAPI, or `curl` of `/api/v1` as the visitor path. The API GET in doctor is a host-health check only.
- Capture the action and the resulting state: Maestro debug screenshots plus the JUnit row for that flow.
- Copy runner output from `src/QueenZone.Mobile/maestro-results/` into the feature artifact folder. Leave the originals if the smoke script wrote them.
- For list → detail, the flow must open the story/photo/thread, not only land on the tab.
- Side effects on this host are in-memory only.

## Cleanup

```powershell
pwsh -File .cursor/skills/verify-queenzone-mobile/scripts/control-queenzone-mobile.ps1 cleanup
```

The helper kills the process tree of the PID it recorded, then deletes `.run/state.json` and `.run/host.json`. It never kills by process name. It never kills the emulator. It never deletes `artifacts/`.

If launch failed after starting a process, run cleanup before the next attempt.

## Helpers

All commands below are from the repository root.

```powershell
pwsh -File .cursor/skills/verify-queenzone-mobile/scripts/control-queenzone-mobile.ps1 launch
pwsh -File .cursor/skills/verify-queenzone-mobile/scripts/control-queenzone-mobile.ps1 doctor
pwsh -File .cursor/skills/verify-queenzone-mobile/scripts/control-queenzone-mobile.ps1 url
pwsh -File .cursor/skills/verify-queenzone-mobile/scripts/control-queenzone-mobile.ps1 drive -Flow news
pwsh -File .cursor/skills/verify-queenzone-mobile/scripts/control-queenzone-mobile.ps1 cleanup
```

`launch` imports the Six Labors licence via `scripts/Import-SixLaborsLicense.ps1` when `SIXLABORS_LICENSE_KEY` is unset.

The repo's existing device-smoke entry point remains `scripts/run-mobile-device-smoke.sh`. Use it when you need a rebuild and reinstall. Use this helper when the host should stay up across a single mapped flow.
