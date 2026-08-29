# Release asset manifest

| Deliverable | Status | Provenance / action |
| --- | --- | --- |
| English (Australia) metadata | Draft complete | Written from implemented mobile screens and QueenZone product positioning |
| Privacy-label draft | Draft complete | Source audit; must be reconciled with final binary/SDK configuration |
| App Review notes | Draft complete | Contains contact and reviewer-account placeholders |
| Screenshot shot list | Complete | Uses actual implemented screen inventory |
| iPhone final screenshots | Blocked on release-candidate capture | Requires installed simulator runtime or TestFlight device |
| iPad final screenshots | Blocked on release-candidate capture | Required while `supportsTablet: true` |
| Release icon | Complete | `assets/icon/QueenZone-AppStore-1024.png`; deterministic alpha removal, exact visible-pixel comparison passed |
| App Store Connect record / draft | Not changed by this pack | Load only after product-owner review and authenticated App Store Connect access |

## Icon provenance

- Source asset: `src/QueenZone.Mobile/assets/icon.png`
- Source dimensions: 1024 × 1024
- Visual: white QueenZone “Q” on `#111111`
- Source PNG declares an alpha channel, although ImageMagick reports every source pixel as opaque.
- Final export: `assets/icon/QueenZone-AppStore-1024.png`, 1024 × 1024, PNG truecolour type 2 (no alpha).
- Verification: ImageMagick absolute-error comparison against the source composited on `#111111` returned **0 changed pixels**.
- A generative flattening attempt was rejected because it subtly altered the mark and is not included in this pack.
