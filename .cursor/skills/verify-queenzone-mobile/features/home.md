# Home

Home is the signed-out landing tab. A visitor sees the archive hero and can open the seeded modernisation story without changing tabs first.

## Sub-features

- `home-launch` shows `home-screen` and `home-hero` after a cold start.
- `home-open-hero` opens story 1003 from the hero.
- `home-return` returns to Home via `tab-home`.

## How to get to it (user POV)

- Launch the installed Debug app.
- Choose the Home tab (`tab-home`) from any other tab.
- Choose the hero card on Home.

## Driving it with Maestro

Preconditions:

- Contract host is healthy at `http://127.0.0.1:5098`.
- `control-queenzone-mobile.ps1 doctor` reports news 1003.
- `org.queenzone.mobile` is installed and the emulator is booted.

- **Cold start.** Run `pwsh -File .cursor/skills/verify-queenzone-mobile/scripts/control-queenzone-mobile.ps1 drive -Flow launch`. `home-screen` and `home-hero` become visible.
- **Open hero.** Run `drive -Flow home`, or `tapOn` `home-hero`. `news-story-screen` is visible and the title `QueenZone modernisation begins` is visible.
- **Return.** `tapOn` `tab-home`. `home-screen` is visible again.
- **Proof.** Keep the Maestro debug screenshots and JUnit row in `artifacts/home/`.

## Gotchas

- A production-baked APK will hang on the hero because it cannot reach `10.0.2.2:5098`.
- `01-launch.yaml` clears app state. Run it before a dirty session, not between every sub-feature.
- The hero uses `home-hero`, not the article title, as the tap target.
