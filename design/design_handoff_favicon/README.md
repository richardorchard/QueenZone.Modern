# Queenzone Favicon — Implementation Handover

Everything needed to ship the Queenzone site favicon. The website shares the native app's primary Cinzel “Q” mark so browser tabs, saved shortcuts and installed experiences use one identity.

Files in this folder:
- `favicon.ico` — multi-resolution 16/32/48px browser icon
- `favicon-32.png` — 32×32 (standard browser tab)
- `favicon-16.png` — 16×16 (small tab / bookmark bar)
- `apple-touch-icon.png` — 180×180 (iOS home-screen, also used by many PWA installs)
- `favicon-512.png` — 512×512 (PWA manifest / app icon source)
- `README.md` — this document

---

## 1. The mark

A **pure-white Cinzel “Q” monogram on rich black `#111111`**, identical to the native app icon. The square source has no pre-rounded corners; browsers and operating systems apply their own masks where appropriate. The full Queen crest remains too intricate to read reliably at favicon sizes.

- Background: Rich Black `#111111` (`--qz-black`).
- Mark: pure white, with no gradient, keyline, bevel or shadow.
- Safe area and placement match the native app icon exactly.

---

## 2. Install

Place the five icon files at your web root (or a `/assets/` path) and add to `<head>`:

```html
<link rel="icon" href="/favicon.ico" sizes="any">
<link rel="icon" type="image/png" sizes="32x32" href="/favicon-32.png">
<link rel="icon" type="image/png" sizes="16x16" href="/favicon-16.png">
<link rel="apple-touch-icon" href="/apple-touch-icon.png">
```

Browsers can use the multi-resolution ICO or explicit PNGs. iOS/iPadOS use `apple-touch-icon.png`. Adjust the `href` paths to wherever you host the files.

### Optional — PWA / Android install
If the site has a web app manifest, reference the large icon:

```json
{
  "icons": [
    { "src": "/favicon-512.png", "sizes": "512x512", "type": "image/png" },
    { "src": "/apple-touch-icon.png", "sizes": "180x180", "type": "image/png" }
  ],
  "theme_color": "#111111",
  "background_color": "#111111"
}
```

## 3. Regenerating / editing

The native app icon at `src/QueenZone.Mobile/assets/icon.png` is the single source of truth. Flatten it onto `#111111`, remove alpha and export 16/32/180/512px PNGs plus a 16/32/48px ICO. Preserve the mark's geometry, scale and position exactly.

---

*Source assets live in the design system at `assets/favicon/` (with a live preview card, `favicon.html`, in the Brand group).*
