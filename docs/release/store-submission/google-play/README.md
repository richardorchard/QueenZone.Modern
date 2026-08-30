# QueenZone Google Play submission pack

Copy-ready listing, policy and release material for `org.queenzone.mobile`.

## Included

- [`store-listing-en-AU.md`](store-listing-en-AU.md) — title, descriptions, categorisation and contact fields.
- [`data-safety.md`](data-safety.md) — conservative draft of Google Play's Data safety form.
- [`app-content-and-review.md`](app-content-and-review.md) — ads, app access, target audience, ratings, news and policy declarations.
- [`release-and-versioning.md`](release-and-versioning.md) — internal testing and first-production workflow.
- [`screenshot-plan.md`](screenshot-plan.md) — final capture list and exact requirements.
- [`manifest.md`](manifest.md) — deliverable state and asset provenance.
- [`assets/icon/QueenZone-PlayStore-512.png`](assets/icon/QueenZone-PlayStore-512.png) — 512px store icon.
- [`assets/feature-graphic/QueenZone-FeatureGraphic-1024x500.png`](assets/feature-graphic/QueenZone-FeatureGraphic-1024x500.png) — final-size feature graphic.

## Draft workflow

The app and internal testing pipeline already exist. Store listing and App content answers can be saved before a production release is chosen. Store-listing assets are shared across testing tracks, so treat uploads as visible to testers even while production is closed.

Do not select **Send for review**, start production rollout or change managed publishing as part of metadata preparation.

## Current blockers to a complete submission

- Final phone screenshots from the chosen release candidate.
- Final reviewer contact and dedicated test credentials.
- Product-owner confirmation of target age groups, content-rights answers and news-app declaration.
- Final Data safety reconciliation against the release AAB and Sentry configuration.
- Production listing review; internal-track `versionName` is `{prefix}.{run}` (`0.1` until the committed prefix flips to `1.0`).

