# Queenzone — Biography & Discography Templates · Handover

A practical brief for the designer/AI picking these up. Assumes you already have the Queenzone core site + design language. This covers only what's specific to these two archive templates.

---

## 1. What these are

Two self-contained page templates that live in the Queenzone design system:

- **Biography** (`biography/Biography.dc.html`) — the Queen story as an editorial timeline. A chapter list opens into long-form chapter pages (drop-cap intro, pull-quote, "Key Moments" event list, prev/next chapter nav).
- **Discography** (`discography/Discography.dc.html`) — the studio-album archive. A sleeve grid (or editorial list) opens into a cinematic album detail page with full tracklist + record-details sidebar.

Each is a **Design Component** (`.dc.html`): a single file that opens directly in a browser and is built from inline-styled markup plus a small logic class. They consume the design system's tokens via `ds-base.js` — no separate stylesheet or build step.

Each folder is fully portable. The four files per folder:
- `<Name>.dc.html` — the template (markup + data + logic)
- `ds-base.js` — loads the design system's CSS + component bundle
- `support.js` — the DC runtime (do not edit)
- `.thumbnail` — picker preview

---

## 2. Both share one structure

A **list/index view** and a **detail view**, toggled by internal state (`view: 'list' | 'detail'`) — no routing, no external data. Clicking an item opens detail; a back link returns to the list; the page scrolls to top on every transition.

All content lives in a single `data` array inside each file's `<script data-dc-script>` logic class. **To re-skin for other content, you mostly just edit that array** — the markup adapts to whatever you put in.

Layout language shared across both:
- Black hero band (`--qz-black`) with a faint crest watermark (`assets/crest-white.png`) and a gold eyebrow label.
- Hairline-ruled rows/cards on warm white.
- `--font-display` for headings, `--font-titling` for eyebrows/numerals/labels, `--font-body` for prose.
- Numerals/labels are uppercase, letter-spaced; everything is centered to `--container-max` / `--container-text`.

---

## 3. Tweakable props

Exposed via the DC's `data-props` (these surface as Tweaks; they are the only "knobs" beyond editing copy directly):

**Biography**
- `numerals` — `roman` | `arabic` (default `roman`). Chapter numbering style.

**Discography**
- `layout` — `grid` | `list` (default `grid`). Sleeve grid vs. editorial row list for the index view.

Preview size for both: 1280 × 900.

---

## 4. Data shapes (edit these to change content)

**Biography** — array of chapters:
```js
{
  range:   "1970–1973",      // shown as the big chapter marker
  title:   "The Formation…",
  roma:    "I",               // roman numeral (arabic auto-derived from index)
  summary: "…",               // list teaser + detail sub-head
  quote:   "…",               // pull-quote
  quoteBy: "On the name…",    // pull-quote attribution
  readtime:"6 min read",
  body:    ["para 1", "…"],   // first para auto gets the drop-cap
  events:  [{ year:"1971", text:"…" }]   // "Key Moments" list
}
```

**Discography** — array of albums (order = catalogue number, auto `01`, `02`…):
```js
{
  title:   "A Night at the Opera",
  year:    1975,
  label:   "EMI · Elektra",
  era:     "The Opera Years",  // shown in the detail sidebar
  summary: "…",                // list-layout description
  blurb:   "…",                // detail hero description
  tracks:  ["Track one", "…"]  // numbered tracklist; count is auto
}
```

Derived automatically (don't hand-set): chapter arabic numerals, album catalogue numbers, track numbers/counts, prev/next titles, the "N chapters / N albums" count line.

---

## 5. Tokens in play

These come from the design system (`styles.css`) — reuse, don't hardcode:

- Colour: `--qz-black`, `--qz-white`, `--qz-warm-white`, `--qz-charcoal`, `--qz-gold`, the `--qz-grey-50…700` ramp, `--accent-archive`, `--hairline`, `--text-primary/secondary/muted`, `--link/--link-hover`.
- Type: `--font-display`, `--font-titling`, `--font-body`.
- Layout/effect: `--container-max`, `--container-text`, `--gutter-lg`, `--radius-md`, `--shadow-card`, `--dur-fast/base`, `--ease-out`.

Album sleeves and the discography detail art are **diagonal-stripe placeholders**. Swap them for real sleeve imagery when available — replace the `repeating-linear-gradient` block with an `<img>` (keep the `aspect-ratio:1/1` frame).

---

## 6. Notes & gotchas

- **Styling is inline only** by design (so the page paints instantly while streaming). Keep new styles inline; the only global CSS lives in the `<helmet><style>` block (resets + the `qzRise` entrance keyframe, which already respects `prefers-reduced-motion`).
- **Markup is directly editable** — headings, paragraphs, list items are real DOM, so copy can be edited in place. Logic/derived values live in the class.
- **No external data/network.** Everything ships in the file.
- The crest watermark and (eventual) sleeve art are the only image assets; everything else is type + rules.
- Both templates are responsive via `clamp()` and flex/grid `wrap` — no fixed breakpoints to maintain.

---

*Files: `templates/biography/`, `templates/discography/`. Open either `.dc.html` in the browser to preview.*
