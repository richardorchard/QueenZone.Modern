# Photography

Photography lets a visitor browse restored image collections, open the Brian May gallery, and inspect a seeded photograph.

## Sub-features

- `photo-index` shows the Photography heading and collection cards including Brian May.
- `photo-category` lists images on `/photography/brian-may`.
- `photo-detail-101` opens `/photography/brian-may/101` with title `Brian in action with his guitar`.

## How to get to it (user POV)

- Open `/photography`.
- Choose `Photography` from the Archive navigation group.
- Open the Brian May collection card.
- Open `/photography/brian-may/101` from a thumbnail.

## Driving it with the browser

Preconditions:

- QueenZone is healthy at `http://127.0.0.1:5199`.
- `control-queenzone.ps1 doctor` reports the Testing host.

- **Open index.** Navigate to `/photography`. The level-1 heading `Photography` is visible. A collection card heading `Brian May` is visible.
- **Open collection.** Choose `Brian May` or navigate to `/photography/brian-may`. The Brian May collection is visible and includes a path to photo 101.
- **Open photo.** Navigate to `/photography/brian-may/101` or choose that image. Visible title text is `Brian in action with his guitar`.
- **Proof.** Capture the photo detail to `artifacts/photography/detail.aria.txt` and `artifacts/photography/detail.png`. Both identify Brian May and the photo title.

## Gotchas

- Sample photo 103 has unknown original dimensions. Do not assert a `0 × 0` size label; its absence is expected.
- Image bytes may 404 if the sample file is not on disk. The page title and collection chrome are still the proof of the visitor route.
- Size-filter query strings (`?size=desktop`) are a separate behavior. Do not require them for this map.
