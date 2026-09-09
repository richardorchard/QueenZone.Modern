# Live mobile screenshot capture

The checked-in captures were taken on 9 September 2026 from production-configured release builds against `https://www.queenzone.org`. Remote artwork was allowed to finish loading before each capture.

## Capture sets

- iOS capture archive: `apple/assets/screenshots/raw-ios/iphone-15-pro-max-1290x2796/` (1290 × 2796, RGB PNG)
- iOS upload set: `apple/assets/screenshots/upload-ready/` (10 files, the App Store maximum)
- Android capture archive: `google-play/assets/screenshots/raw-android/phone-1080x1920/` (1080 × 1920, RGB PNG)
- Android upload set: `google-play/assets/screenshots/upload-ready/` (8 files, the Play Console maximum per device type)

The complete numbered sequence covers Home, News, Photography, Archive, Forum, Quote, Discography, Fan performances, Trivia, Biography, photo gallery, and photo viewer. The upload-ready directories contain the strongest allowed subset for each store.

These are clean emulator captures at the final store dimensions. They are not stretched or upscaled.

## Maestro workflow

Install a production-configured release build on the target emulator or simulator, then run:

```sh
maestro --device <device-id> test src/QueenZone.Mobile/maestro/store-screenshots-public.yaml
```

For iOS, use the **iPhone 15 Pro Max** Simulator profile. On the current Xcode runtime its native screenshot output is 1290 × 2796. The iPhone 16 Pro Max and iPhone 17 Pro Max profiles output 1320 × 2868 instead. QueenZone is iPhone-only; do not produce or upload iPad screenshots.

For Android, set and verify the emulator display before capture:

```sh
adb shell wm size 1080x1920
adb shell wm density 420
adb exec-out screencap -p > size-check.png
magick identify size-check.png
```

For the authenticated capture, load the reviewer credentials from Bitwarden Secrets Manager into environment variables without printing them, then run:

```sh
REVIEWER_EMAIL='…' REVIEWER_PASSWORD='…' \
  maestro --device <device-id> test src/QueenZone.Mobile/maestro/store-screenshots-authenticated.yaml
```

Never commit credentials, Maestro debug artifacts, or an inbox screenshot containing real private content. The iOS remaining-screen flow handles native-stack back-navigation differences and is also safe to use on Android.

Run `scripts/validate-store-screenshots.sh` before upload or commit. It checks counts, exact dimensions, PNG colour type and alpha-channel absence.

Widget screenshots remain a separate manual capture: add the installed widget to a clean simulator Home Screen, remove personal widgets and notifications, and follow the platform screenshot plan.
