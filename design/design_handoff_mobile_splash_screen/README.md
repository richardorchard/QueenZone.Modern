# Queenzone Mobile Splash Screen — Implementation Handover

Design source: `Splash Screen.dc.html` in the original design pack (390 × 844 reference frame). Design system: Queenzone DS — Rich Black `#111111`, crest emblem, Cinzel titling, Inter meta.

Files in this folder:
- `crest-emblem.png` — crest emblem, white, wordmark cropped off (395 × 331), used for both the hero mark and the large background watermark
- `icons/*.png` — 192 / 512 / maskable-512 / apple-touch-180 (for a future PWA/web manifest, not consumed by the native app)
- `splash/*.png` — 11 fully-composited static launch images at iOS device pixel sizes (design reference only — see "Why no static launch images" below)
- `manifest.json`, `head-snippet.html` — web app manifest + meta/link tags (for a future PWA/web shell, not consumed by the native app)

## Two splashes, one design

1. **OS splash (cold start).** Both iOS and Android render this via the `expo-splash-screen` config plugin in `src/QueenZone.Mobile/app.json` (`image`, `backgroundColor: "#111111"`, `resizeMode: "contain"`). The image is `src/QueenZone.Mobile/assets/splash-icon.png` — replaced with `crest-emblem.png` from this pack (it previously held Expo's default placeholder icon).
2. **In-app splash.** `src/QueenZone.Mobile/src/splash/BootSplash.tsx` renders as soon as the JS bundle mounts and covers the gap while fonts load / the app boots. It shares the same black field and centred crest as the OS splash, so the handover reads as one continuous screen. Wired into `App.tsx`: shown full-screen while fonts are loading, then stays mounted as an overlay and fades out once the app is ready.

### Why no static launch images
The `splash/*.png` set in this pack follows the classic PWA / `apple-touch-startup-image` pattern (one baked PNG per device resolution), which only applies to a Safari "Add to Home Screen" web app. The QueenZone mobile app is a native Expo app — both platforms generate the native launch screen from a single vector-friendly image + background colour via `expo-splash-screen`, so the per-device PNGs aren't wired up anywhere. They're kept here as the exact pixel-accurate design reference (crest position, watermark opacity, type sizes) for anyone touching `BootSplash.tsx`.

## Implementation notes (as built)

- `BootSplash` renders as an overlay (`position: absolute`, full-bleed, `zIndex: 100`) and fades out over 320ms once the app is ready — it isn't unmounted instantly.
- Minimum on-screen time 600ms so it never flickers; hard ceiling 2.5s, after which the app shell shows regardless of boot state.
- Motion: staggered rise/fade (crest → wordmark → footer), easing `cubic-bezier(0.22, 0.61, 0.36, 1)` (matches `motion.easing` in `src/theme/tokens.ts`), durations 900/1200ms. `AccessibilityInfo.isReduceMotionEnabled()` collapses all animation to near-instant.
- Loader is an indeterminate 1px hairline wipe — no spinners, no percentages.
- Footer disclaimer text reuses the existing `archiveDisclaimer` token (already shared with `ArchiveFooter`).

## Type & colour used

- Wordmark: Cinzel 400, 27px, letter-spacing `0.26em`, uppercase, `#FFFFFF` (falls back to system serif until the Cinzel font finishes loading)
- Eyebrow/tagline: Cinzel 400, 9.5px, `0.32em`, `rgba(255,255,255,0.6)`
- Disclaimer: Inter 400, 10.5px, `rgba(255,255,255,0.38)`
- Field: `#111111`; crest watermark at ~5–6% opacity (`dark.crestWatermarkOpacity`); hairline rule `rgba(255,255,255,0.28)`

## Still open / worth deciding

- **Light-mode variant** — deliberately not made; the splash is dark on all themes, which is the more on-brand read.
- **Higher-res crest** — the source is ~447px square; if the mobile app ever needs a larger hero render, a vector or ≥1024px crest would sharpen it.
- **PWA/web shell** — `manifest.json`, `head-snippet.html`, `icons/`, and `splash/` are unused until a web/PWA wrapper exists; wire them up then per the notes above.
