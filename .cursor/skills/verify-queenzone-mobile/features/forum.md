# Forum

Forum shows public boards, opens The Music, and reads the seeded ranking thread.

## Sub-features

- `forum-tab` shows `forum-screen` after `tab-forum`.
- `forum-board-1` opens The Music (`forum-board-1`).
- `forum-thread-1002` shows `Ranking every studio album` on `forum-thread-screen`.

## How to get to it (user POV)

- Choose the Forum tab.
- Choose The Music.
- Choose Ranking every studio album.

## Driving it with Maestro

Preconditions:

- Contract host is healthy at `http://127.0.0.1:5098`.
- `control-queenzone-mobile.ps1 doctor` reports the Testing host.
- The app is installed on a booted emulator.

- **Open thread.** Run `drive -Flow forum`. The flow taps `tab-forum`, `forum-board-1`, `forum-thread-1002`, and asserts `Ranking every studio album`.
- **Return.** The flow taps `tab-forum` so the board list is visible again.
- **Proof.** Keep the thread screen in `artifacts/forum/`.

## Gotchas

- Board and thread ids are `forum-board-${id}` and `forum-thread-${id}`. 1 and 1002 are the stable samples.
- `forum-new-thread` opens sign-in when signed out. That is covered by `08-profile-signed-out.yaml`, not this file.
- Signed-in posting is out of scope for this map.
