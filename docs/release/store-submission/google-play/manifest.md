# Google Play deliverable manifest

| Deliverable | Status | File / action |
| --- | --- | --- |
| English (Australia) listing | Draft complete | `store-listing-en-AU.md` |
| Data safety answers | Draft complete | `data-safety.md`; reconcile with final AAB |
| App content / access answers | Draft complete | `app-content-and-review.md`; private credentials pending |
| Release/versioning notes | Complete | `release-and-versioning.md` |
| Play Store icon | Complete | `assets/icon/QueenZone-PlayStore-512.png` |
| Feature graphic | Complete | `assets/feature-graphic/QueenZone-FeatureGraphic-1024x500.png` |
| Phone screenshot plan | Complete | `screenshot-plan.md` |
| Final phone screenshots | Pending release candidate | Capture six real 1080 × 1920 images |
| Tablet screenshots | Deferred pending large-screen QA | Do not submit phone upscales |
| Reviewer contact/account | Pending private entry | Never commit credentials |
| Play Console listing draft | Not changed by this pack | Load only after product-owner review |

## Asset provenance

### Store icon

- Source: `src/QueenZone.Mobile/assets/icon.png`.
- Export: 512 × 512 PNG.
- Exact white Q on `#111111`; deterministic ImageMagick resize from the 1024px mobile source.
- No text, badge, rounding or added effect.
- Fully opaque and below Play's 1024KB limit.

Google currently specifies a 32-bit PNG for the Play icon. The export is intentionally fully opaque even though PNG supports alpha, matching the square launcher artwork and avoiding accidental transparency.

### Feature graphic

- Export: 1024 × 500, 24-bit PNG, no alpha.
- Exact copy: `QUEENZONE`, `THE ARCHIVE · PRESERVED`, `HISTORY • NEWS • PHOTOGRAPHY • COMMUNITY`.
- Uses repository Cinzel and Inter fonts plus the existing gold/burgundy brand palette.
- Contains no third-party photography, Queen crest, award, ranking, price, store badge, call to action or time-sensitive claim.
- Central focal point and decorative edge motifs are designed to tolerate Play promotional cropping.

## Mechanical validation

Run from the repository root:

```bash
magick identify -format '%f %wx%h opaque=%[opaque] type=%[type]\n' \
  docs/release/store-submission/google-play/assets/icon/QueenZone-PlayStore-512.png \
  docs/release/store-submission/google-play/assets/feature-graphic/QueenZone-FeatureGraphic-1024x500.png
```

Expected dimensions are `512x512` and `1024x500`; both must report `opaque=True`.

