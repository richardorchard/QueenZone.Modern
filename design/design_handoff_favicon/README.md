# Queenzone Favicon — Implementation Handover

Everything needed to ship the Queenzone site favicon. Drop these files on the server and paste the `<head>` snippet.

Files in this folder:
- `favicon.svg` — scalable master (modern browsers; auto-crisp at any size)
- `favicon-32.png` — 32×32 (standard browser tab)
- `favicon-16.png` — 16×16 (small tab / bookmark bar)
- `apple-touch-icon.png` — 180×180 (iOS home-screen, also used by many PWA installs)
- `favicon-512.png` — 512×512 (PWA manifest / app icon source)
- `README.md` — this document

---

## 1. The mark

An **antique-gold "Q" monogram on a rich-black rounded tile**, with a faint gold inner keyline. The full Queen crest is too intricate to read at 16px, so the favicon distils the brand to its initial — consistent with the design system's ~90% monochrome / gold-as-rarest-accent rule.

- Tile: Rich Black `#111111` (`--qz-black`), 22%-radius rounded square.
- Mark + keyline: Antique Gold `#B89A4A` (`--qz-gold`), with a subtle vertical gold gradient (`#D9BE6E → #B89A4A → #9C8038`) for a metallic read at large sizes.
- Safe area: the Q sits within ~70% of the tile; the rest is padding so it survives platform masking.

---

## 2. Install

Place the five icon files at your web root (or a `/assets/` path) and add to `<head>`:

```html
<link rel="icon" type="image/svg+xml" href="/favicon.svg">
<link rel="icon" type="image/png" sizes="32x32" href="/favicon-32.png">
<link rel="icon" type="image/png" sizes="16x16" href="/favicon-16.png">
<link rel="apple-touch-icon" href="/apple-touch-icon.png">
```

Browsers that support SVG favicons use `favicon.svg`; the PNGs are the fallback. iOS/iPadOS use `apple-touch-icon.png`. Adjust the `href` paths to wherever you host the files.

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

### Optional — legacy `favicon.ico`
Modern browsers don't need one. If you support very old clients, generate a multi-resolution `favicon.ico` (16/32/48) from `favicon-512.png` with any icon tool (e.g. ImageMagick: `convert favicon-512.png -define icon:auto-resize=48,32,16 favicon.ico`) and add `<link rel="icon" href="/favicon.ico" sizes="any">`.

---

## 3. Regenerating / editing

`favicon.svg` is the single source of truth — edit it and re-export the PNGs at 16/32/180/512. Keep the geometry inside the safe area and the colours on the two brand tokens (`#111111`, `#B89A4A`). If the brand ever wants a light-tab variant, invert to a black Q on a warm-white (`#F7F5F0`) tile rather than recolouring the gold.

---

*Source assets live in the design system at `assets/favicon/` (with a live preview card, `favicon.html`, in the Brand group).*
