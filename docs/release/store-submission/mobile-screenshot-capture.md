# Live mobile screenshot capture

The checked-in raw captures were taken on 8 September 2026 from production-configured release builds against `https://www.queenzone.org`. Remote artwork was allowed to finish loading before each capture.

## Capture sets

- iOS: `apple/assets/screenshots/raw-ios/iphone-17-pro-max/` (1320 × 2868, RGB PNG)
- Android: `google-play/assets/screenshots/raw-android/pixel-8/` (1080 × 2400, RGB PNG)

The numbered sequence covers Home, News, Photography, Archive, Forum, Quote, Discography, Fan performances, Trivia, Biography, photo gallery, and photo viewer. Android also includes a signed-in Private messages capture showing the empty Archived view. It intentionally contains no member names or message content.

These files are clean raw emulator captures. Select and compose the final store-sized marketing set according to each platform's `screenshot-plan.md`; do not upscale or crop these originals destructively.

## Maestro workflow

Install a production-configured release build on the target emulator or simulator, then run:

```sh
maestro --device <device-id> test src/QueenZone.Mobile/maestro/store-screenshots-public.yaml
```

For the authenticated capture, load the reviewer credentials from Bitwarden Secrets Manager into environment variables without printing them, then run:

```sh
REVIEWER_EMAIL='…' REVIEWER_PASSWORD='…' \
  maestro --device <device-id> test src/QueenZone.Mobile/maestro/store-screenshots-authenticated.yaml
```

Never commit credentials, Maestro debug artifacts, or an inbox screenshot containing real private content. The iOS extras flow exists as a targeted fallback for capturing Biography when native-stack back navigation differs from Android.

Widget screenshots remain a separate manual capture: add the installed widget to a clean simulator Home Screen, remove personal widgets and notifications, and follow the platform screenshot plan.
