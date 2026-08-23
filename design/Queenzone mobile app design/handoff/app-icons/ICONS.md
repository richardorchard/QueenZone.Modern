# Queenzone — app icons

*Version 1 · 22 August 2026. Part of the Queenzone mobile app handoff pack.*

Two icon marks are supplied. **The primary set is the Cinzel "Q" monogram.** Use it unless the
project decides otherwise — see §1 for why.

---

## 1. Why the monogram is primary

The obvious choice was the crest. It does not survive the small sizes an app icon actually lives
at. Open `icon-size-comparison.png`: the crest is a fine line-art emblem, and below roughly 120px
its lions, wings and coronet collapse into grey mush — at 40px (Spotlight, Settings, notification)
it is an unreadable smudge. The Cinzel Q holds its shape at every size, is unmistakably *this*
brand (Cinzel is the design system's titling face), and reads as an editorial imprint rather than
a band logo.

The crest set is still supplied, in `alternate-crest/`, at 120px and above only. It is the right
mark for a splash screen, an About screen, or marketing — not for the launcher.

**One practical caution to settle before submission:** the crest is Queen's own emblem. Using it
as the launcher icon of a third-party fan application is a trademark question, not a design one —
worth a decision by the project owner (and it is a common cause of App Store review rejection).
The monogram avoids the issue entirely.

## 2. Specification

Both marks use the same construction:

| | Value |
|---|---|
| Background | Rich Black `#111111` (the app's `surfacePage`) |
| Mark | Pure white, no gradient, no shadow, no bevel |
| Accent | None. The gold is deliberately absent — an icon has no state to signal |
| iOS art fraction | 60% of the canvas (monogram) / 70% (crest) |
| Android art fraction | 40% of the 432px layer — inside the 66dp adaptive safe zone |
| Corners | **Square, unrounded.** Both platforms apply their own mask. Never pre-round |
| Transparency | iOS icons are fully opaque (App Store rejects alpha). Android foreground/monochrome layers are transparent by design |

## 3. Files

```
app-icons/
├── ios/                          Monogram — PRIMARY
│   ├── AppIcon-1024.png          App Store / Xcode single-size asset
│   └── AppIcon-{180,167,152,120,87,80,60,58,40}.png
├── android/                      Monogram — PRIMARY
│   ├── ic_launcher_background.png    432×432 solid #111111
│   ├── ic_launcher_foreground.png    432×432 transparent, mark in safe zone
│   ├── ic_launcher_monochrome.png    432×432 for Android 13+ themed icons
│   ├── play-store-512.png            512×512 flattened listing icon
│   └── legacy/mipmap-{mdpi…xxxhdpi}/ic_launcher.png   API < 26 fallback
├── alternate-crest/              Crest — large sizes only (≥120px)
│   ├── ios/AppIcon-{1024,180,167,152,120}.png
│   └── android/{background,foreground,monochrome,play-store-512}.png
├── icon-size-comparison.png      The legibility evidence
├── ios-Contents.json             Drop-in asset catalogue manifest
├── android-ic_launcher.xml       Adaptive icon definition
├── android-ic_launcher_monochrome.xml
├── android-colors.xml            ic_launcher_background colour
└── expo-app.json.snippet         Expo / app config block
```

## 4. Install — iOS (bare React Native / Xcode)

Modern Xcode (14+) needs only the 1024 asset.

1. In Xcode, open `Images.xcassets` → `AppIcon`.
2. Drag `ios/AppIcon-1024.png` into the **1024pt** "Any Appearance" slot. Leave Dark and Tinted
   empty — the mark is already dark-native and reads correctly under the tinted treatment.
3. Confirm **App Icon** → *Single Size* is selected in the attributes inspector.

For a legacy multi-size catalogue, copy `ios-Contents.json` to
`ios/<App>/Images.xcassets/AppIcon.appiconset/Contents.json` and drop all nine `AppIcon-*.png`
files alongside it.

Checks: no alpha channel, no rounded corners, no drop shadow, exactly 1024×1024.

## 5. Install — Android (bare React Native / Android Studio)

1. Copy the three 432px layers into `android/app/src/main/res/drawable/`:
   `ic_launcher_background.png`, `ic_launcher_foreground.png`, `ic_launcher_monochrome.png`.
2. Copy `android-ic_launcher.xml` → `res/mipmap-anydpi-v26/ic_launcher.xml`
   **and** `res/mipmap-anydpi-v26/ic_launcher_round.xml` (same content).
3. Copy `android-colors.xml` contents into `res/values/colors.xml`.
4. Copy `android/legacy/mipmap-*/ic_launcher.png` into the matching
   `res/mipmap-*/` folders for API < 26.
5. `AndroidManifest.xml` should already carry
   `android:icon="@mipmap/ic_launcher"` and `android:roundIcon="@mipmap/ic_launcher_round"`.

Verify in Android Studio's **Image Asset** preview that nothing important is clipped by the
circle, squircle, or rounded-square masks — the mark sits inside the safe zone, so it should not
be.

## 6. Install — Expo

Copy the block in `expo-app.json.snippet` into `app.json`, and place the referenced files under
`assets/icon/`. Expo generates every derived size at build time from the 1024 and the adaptive
layers, so the per-size PNGs are not needed on this path.

## 7. Notification icon (Android)

Android status-bar icons are a **silhouette** — any colour information is discarded.
`ic_launcher_monochrome.png` doubles as the source: create
`res/drawable/ic_notification.png` at 96×96 with the white mark on transparent, ~24dp of padding.
Set `android:color` on the notification builder to `#B89A4A` so the tint is the brand gold.

## 8. Regenerating

The masters are `assets/crest-white.png` (crest) and Cinzel 600-weight "Q" (monogram), both
composed on `#111111`. If a size needs re-cutting, scale from the 1024 — never up from a small
one. Keep the art fraction and the square corners; those two decisions are the whole spec.
