# News

News lists published articles on the News tab and opens the seeded modernisation story.

## Sub-features

- `news-tab` shows `news-screen` after choosing `tab-news`.
- `news-open-1003` opens `news-story-1003`.
- `news-title` shows `QueenZone modernisation begins` on `news-story-screen`.

## How to get to it (user POV)

- Choose the News tab.
- Choose the row for QueenZone modernisation begins.
- Open the same story from the Home hero (see `home.md`).

## Driving it with Maestro

Preconditions:

- Contract host is healthy at `http://127.0.0.1:5098`.
- `control-queenzone-mobile.ps1 doctor` reports news 1003.
- The app is installed on a booted emulator.

- **Open tab.** Run `pwsh -File .cursor/skills/verify-queenzone-mobile/scripts/control-queenzone-mobile.ps1 drive -Flow news`. That taps `tab-news`, waits for `news-screen`, taps `news-story-1003`, and asserts the title.
- **Return.** The flow taps `tab-news` again so the list is visible.
- **Proof.** Copy JUnit and debug screenshots to `artifacts/news/`. The story screen, not only the list, must appear.

## Gotchas

- Dynamic rows use `news-story-${id}`. Story 1003 is the only stable sample id for this map.
- `Hidden moderation draft` (web id 9001) must not appear.
- Jest `NewsStoryScreen` tests are not visitor proof.
- The header back control is `news-story-back`. The smoke flow returns via `tab-news` instead.
