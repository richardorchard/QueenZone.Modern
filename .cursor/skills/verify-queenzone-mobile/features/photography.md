# Photography

Photos lets a visitor open the Brian May collection and inspect seeded photo 101.

## Sub-features

- `photos-tab` shows `photos-screen` after `tab-photos`.
- `photos-category` opens `photo-category-brian-may`.
- `photos-item-101` opens `photo-item-101` on `photo-viewer-screen`.

## How to get to it (user POV)

- Choose the Photos tab.
- Choose the Brian May collection.
- Choose photo 101.

## Driving it with Maestro

Preconditions:

- Contract host is healthy at `http://127.0.0.1:5098`.
- `control-queenzone-mobile.ps1 doctor` reports the Testing host.
- The app is installed on a booted emulator.

- **Open collection.** Run `drive -Flow photos`. The flow taps `tab-photos`, `photo-category-brian-may`, then `photo-item-101`, and waits for `photo-viewer-screen`.
- **Return.** The flow taps `tab-photos` so the index is visible again.
- **Proof.** Keep screenshots of the viewer, not only the category grid, in `artifacts/photos/`.

## Gotchas

- Category ids are slugs: `photo-category-brian-may`, not the display name.
- Item ids are `photo-item-${picId}`. Photo 101 is the stable sample.
- Sample image bytes may 404. The viewer chrome and `photo-viewer-screen` are still the proof of the route.
- Do not require pinch-zoom for this map.
