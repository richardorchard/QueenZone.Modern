# Gallery (Photography) — Implementation Handover

Production spec for the **Queenzone photographic archive** template, prototyped in `templates/gallery/Gallery.dc.html`. This package is everything an engineer needs to build it for real. It assumes the Queenzone design system (tokens + component bundle) is already in the app.

Files in this folder:
- `README.md` — this document (structure, behaviour, props, data, tokens, acceptance criteria)
- `gallery-data.js` — the sample categories + images, separated from the component

---

## 1. What it is

A three-level photo archive:

```
Categories index  →  Thumbnail grid (paginated)  →  Lightbox (large view)
```

1. **Categories index** — a dark editorial hero ("Photography") over a card grid of collections (Album Artwork, Live & Stadium, Studio Sessions, Backstage & Candid, Press & Magazines, Memorabilia). Each card: greyscale cover, title, image count, one-line blurb.
2. **Thumbnail grid** — opening a collection shows a dense grid of **small (~150px) square thumbnails**, each with a title + meta caption beneath. Paginated (default 12 per page) with Prev / numbered / Next controls.
3. **Lightbox** — clicking a thumbnail **or its title** opens a full-screen dark overlay: large contained image, counter (`n / total`), prev/next arrows, and a caption block (title, description, gold meta line).

All three are one template, toggled by internal `view` state — no routing, no network. Content lives in one `cats` array (see `gallery-data.js`).

---

## 2. Navigation & state

State: `{ view: 'cats'|'grid'|'detail', catIdx, page, imgIdx }`.

- Category card → `grid` (resets to page 0).
- "All collections" back-link → `cats`.
- Thumbnail / title → `detail` at that image's global index.
- Lightbox back-link → returns to `grid` **on the page that contains the current image** (`floor(imgIdx / perPage)`), not page 0.
- Prev/next in the lightbox step `imgIdx` across the **whole collection**, not just the current page — so paging boundaries are invisible while browsing.
- Every view change scrolls to top (`requestAnimationFrame` + `scrollTo`).

---

## 3. Tweakable prop

| Prop | Editor | Default | Range | Effect |
|---|---|---|---|---|
| `perPage` | int | 12 | 4–24 | Thumbnails per grid page. |

Preview size: 1280 × 900.

Everything else (image counts, page count, range label "Showing 1–12 of 18", prev/next enablement) is **derived** — don't hand-set.

---

## 4. Data shape (edit to change content)

A `cats` array; each category:

```js
{
  name:  'Album Artwork',
  blurb: 'Original sleeves, inner gatefolds and single covers…',  // card sub-line
  cover: 'a',            // key into the image map — the card cover
  shots: [
    // [imgKey, title, meta, caption]
    ['a', 'Innuendo', '1991 · LP sleeve', 'The defiant penultimate album cover…'],
    …
  ],
}
```

- `imgKey` indexes a small image map (`{ a, b, c }` in the prototype — three restored sleeves stand in for the archive). In production replace this with real per-image `src` (full asset URLs or a CDN id).
- `meta` is the gold uppercase line (year · format); `caption` shows only in the lightbox.
- Page count, thumbnail numbering and the counter are computed from `shots.length` + `perPage`.

**Production note:** the prototype reuses three placeholder images for every shot. Real build needs one `src` (plus ideally a separate small `thumbSrc`) per image — thumbnails should load a small derivative, the lightbox the large master. Keep the greyscale (`filter: grayscale(1)`) treatment on thumbs and covers; the lightbox shows the image as-is.

---

## 5. Visual / layout language

- **Hero band:** `--qz-black` with a faint crest watermark (`assets/crest-white.png`), gold eyebrow, display-font title.
- **Category cards:** `aspect-ratio 4/3`, greyscale cover under a bottom-up scrim, title + gold count overlaid; hover lifts 5px.
- **Thumbnails:** `aspect-ratio 1/1`, ~150px min, greyscale, hover lifts 3px; title turns to `--link` on hover. Grid is `repeat(auto-fill, minmax(150px,1fr))`.
- **Pagination:** centred row above a hairline — Prev (disabled state greyed), numbered buttons (current = charcoal fill), Next.
- **Lightbox:** `rgba(12,12,12,0.97)` full-screen flex column: header (back + counter), centre (arrows + contained image, `max-height 72vh`), caption footer. Disabled arrows render as dimmed non-interactive spans at the collection ends.

---

## 6. Tokens used (do not hardcode)

Colour: `--qz-black`, `--qz-white`, `--qz-warm-white`, `--qz-charcoal`, `--qz-gold`, `--qz-grey-50/200/300`, `--accent-archive`, `--hairline`, `--text-primary/secondary/muted`, `--link`.
Type: `--font-display`, `--font-titling`, `--font-body`.
Layout/motion: `--container-max`, `--gutter-lg`, `--radius-sm/md`, `--shadow-sm/card`, `--dur-fast/base`, `--ease-out`.

Styling is **inline by design** (DC templates paint instantly while streaming). The only global CSS is the `<helmet><style>` block: resets + `qzRise`/`qzFade` keyframes, already gated behind `prefers-reduced-motion`.

---

## 7. Production work the prototype doesn't include

- **Real images + responsive sources:** per-image `src`/`thumbSrc`, `srcset`/`sizes`, lazy-loading (`loading="lazy"`) on thumbnails, width/height to prevent CLS.
- **Routing:** map the three views + `catIdx`/`page`/`imgIdx` to real URLs (e.g. `/photography`, `/photography/live-stadium?page=2`, `…/live-stadium/wembley-1986`) so views are shareable and back-button works.
- **Keyboard & a11y in the lightbox:** **Esc** closes to grid; **←/→** step images; focus trap while open; `role="dialog"` + `aria-label`; return focus to the originating thumbnail on close. Arrow links need real focus-visible states.
- **Touch:** swipe left/right in the lightbox; larger tap targets on mobile; consider 2-up thumbnail columns at narrow widths (grid already reflows, but verify min thumb size stays tappable).
- **Loading/empty states:** skeletons for slow scans; graceful empty category.
- Real links/routes replace the `e.preventDefault()` stubs throughout.

---

## 8. Acceptance criteria

- [ ] Three views render from the `cats` data; adding a shot needs no markup change.
- [ ] `perPage` drives pagination; range label + page buttons + counter all stay correct.
- [ ] Lightbox back-link returns to the page containing the current image.
- [ ] Prev/next traverse the whole collection across page boundaries; ends disable cleanly.
- [ ] Title **and** thumbnail both open the lightbox.
- [ ] Full keyboard + screen-reader operation of the lightbox; reduced-motion respected.
- [ ] Thumbnails lazy-load small derivatives; lightbox loads the large master; no CLS.
- [ ] Greyscale treatment on covers/thumbs; lightbox image full-tone.

*Live prototype: `templates/gallery/Gallery.dc.html` — open in a browser, click a collection, page through, open an image.*
