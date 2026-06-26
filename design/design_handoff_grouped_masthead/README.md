# Grouped Masthead — Implementation Handover

Production spec for the **proposed grouped navigation** prototyped in `ui_kits/website/grouped-masthead.html`. This package is everything an engineer needs to build it for real. It assumes the Queenzone design system (tokens + component bundle) is already in the app.

Files in this folder:
- `README.md` — this document (IA, behaviour, props, tokens, acceptance criteria)
- `GroupedMasthead.jsx` — reference React implementation, cleaned for production
- `nav-data.js` — the information-architecture data (the three groups), separated from the component

---

## 1. What this replaces

Today's header is a flat row of five page links (**News · Stories · Photography · Forum · Timeline**). It doesn't scale — every new section (Biography, Discography, Fan Performances) adds another link.

The grouped masthead collapses everything into **three top-level groups**, each opening a quiet table-of-contents dropdown. New sections slot into an existing group; **the bar never grows.**

```
The Band ▾        Archive ▾        Community ▾
```

| Group | Eyebrow | Accent token | Items |
|---|---|---|---|
| **The Band** | About Queen | `--qz-purple` | Biography ·new·, Discography ·new·, Timeline |
| **Archive** | The Publication | `--qz-blue` | News, Stories, Photography |
| **Community** | The Fans | `--qz-burgundy` | Forum, Fan Performances ·new· |

The data lives in `nav-data.js` — adding/moving a section is a data edit, not a markup change.

---

## 2. Anatomy

```
header (sticky, top:0, z-index 50)
└─ container (max var(--container-max), height 76px)
   ├─ Brand      crest mark (42px) + "QUEENZONE" wordmark (titling, 0.18em)
   ├─ nav        three group buttons, each with chevron + hover/click Panel
   └─ Actions    Search IconButton + "Sign in" Button (secondary, sm)
```

**Panel (dropdown):** absolutely positioned under its group button. Eyebrow label (accent colour, uppercase titling) over a hairline, then each item as a row: title + optional `New` gold pill, with a one-line description beneath. Hovered row gets a warm-white background and a 2px accent bar on the left.

---

## 3. Two surface states (the `dark` prop)

The masthead inverts based on what's behind it — this is the brand's signature dark/light rhythm.

| | `dark` (over a black hero) | light (on-page, default) |
|---|---|---|
| Header bg | `--qz-black` → `rgba(17,17,17,0.92)` scrolled | `--qz-white` → `rgba(255,255,255,0.92)` scrolled |
| Wordmark / nav idle | white / `rgba(255,255,255,0.82)` | `--qz-charcoal` |
| Nav active (open) | `--qz-gold` | `--qz-blue` |
| Crest asset | `crest-white.png` | `crest-black.png` |
| Panel surface | `#171717`, white text | `--qz-white`, charcoal text |

In both states a **gilt gold hairline** (`1px solid rgba(184,154,74,0.55)`) sits under the bar — the brand's rarest accent, used as a single editorial rule.

**Scroll behaviour:** past ~12px the bar gains a translucent background, `backdrop-filter: saturate(180%) blur(12px)`, and a soft shadow. Drives off the scroll container, not `window`, if the app scrolls an inner element.

---

## 4. Interaction spec

- **Open on hover** (pointer) with a **130ms close delay** so diagonal travel into the panel doesn't dismiss it. Clearing the timer on re-enter prevents flicker.
- **Open on click/tap** too — toggles the panel. This is the path for touch + keyboard.
- Only one panel open at a time.
- Chevron rotates 180° when its group is open (`--dur-fast`/`--ease-out`).
- Panel entrance: 6px rise + fade (`qzPanel` keyframe, `--dur-fast`).

### Accessibility — required for production (not in the prototype)
The prototype uses bare hover/`<button>`s. The real build **must** add:
- Group buttons: `aria-haspopup="true"`, `aria-expanded`, `aria-controls` → panel `id`.
- **Escape** closes the open panel and returns focus to its button.
- **Arrow Up/Down** moves between items inside an open panel; **Tab** out closes it.
- Focus-visible rings on every interactive element (don't rely on hover-only affordances).
- Respect `prefers-reduced-motion` — disable the rise/chevron transitions.
- Panel links are real `<a href>` with correct routes, not `e.preventDefault()` stubs.

---

## 5. Tokens used (do not hardcode)

Colour: `--qz-black`, `--qz-white`, `--qz-warm-white`, `--qz-charcoal`, `--qz-gold`, `--qz-purple`, `--qz-blue`, `--qz-burgundy`, `--qz-grey-200/500/600`, `--border-on-dark`.
Type: `--font-display`, `--font-titling`, `--font-body`.
Layout/motion: `--container-max`, `--gutter-lg`, `--dur-fast`, `--dur-base`, `--ease-out`.
Components consumed from the bundle: `Button` (secondary/sm), `IconButton` (ghost, `onDark`).

---

## 6. Acceptance criteria

- [ ] Three groups render from `nav-data.js`; adding a 4th item to a group needs no JSX change.
- [ ] Dark and light states match the table in §3, including the gilt hairline in both.
- [ ] Hover opens with 130ms close grace; click/tap toggles; only one panel open.
- [ ] Scrolled state applies translucency + blur + shadow off the correct scroll root.
- [ ] Full keyboard + screen-reader operation per §4; reduced-motion respected.
- [ ] Panel links navigate to real routes; `New` pills are data-driven.
- [ ] No layout shift / CLS when the bar gains its scrolled background.

---

## 7. Mobile / touch behaviour

The prototype is desktop-only. Hover dropdowns don't translate to touch, and the 130ms close-grace logic is meaningless there — mobile uses a **full-screen drawer** instead.

**Bar (mobile):** same sticky header, same dark/light states + gilt hairline. Height drops ~76px → ~60px. Collapse the centre nav into a single **hamburger**; keep crest + wordmark and the **Search** icon. "Sign in" moves into the drawer.

**Menu:** tapping the hamburger slides in a full-height panel over a scrim; body scroll locks. The three groups render as labelled, accent-eyebrow sections (the desktop dropdowns stacked vertically):

```
✕                          Search
─────────────────────────────────
THE BAND            (purple eyebrow)
  Biography           New
  Discography         New
  Timeline
─────────────────────────────────
ARCHIVE             (blue eyebrow)
  News · Stories · Photography
─────────────────────────────────
COMMUNITY           (burgundy eyebrow)
  Forum
  Fan Performances    New
─────────────────────────────────
  Sign in
```

**Layout variants** (pick one):
- **All groups expanded** (recommended) — only ~8 items total, so show everything as labelled sections in one scroll, no extra taps. Eyebrow labels carry the grouping.
- **Accordion** — each group header taps to expand (chevron rotates, one open at a time). Prefer this only if the list grows past ~12 items.

**Behaviour:**
- Full-width rows, tap targets ≥44px; left accent bar; `New` pill right-aligned.
- Same `nav-data.js` drives both desktop and mobile — no separate data.
- Drawer closes on ✕, backdrop tap, or **Escape**; focus trapped while open; respects `prefers-reduced-motion`.
- Drawer surface follows the dark/light state (or always-light — decide per brand; light drawer over a dark page reads cleanly).

Breakpoint: switch from the grouped bar to the hamburger drawer at ≤ ~860px (where the three groups + actions stop fitting comfortably).

---

## 8. Notes

- `GroupedMasthead.jsx` here is the **reference** — same structure as the prototype's `Masthead`/`Panel`, minus the demo scaffolding (the hero/light bands and the dark/light toggle were prototype-only). Wire `dark` from the route (true on pages with a black hero, false elsewhere) and `scrolled` from your scroll listener.
- Keep the panel width comfortable (min ~320px) so two-line descriptions don't wrap awkwardly.
- The prototype loads React/Babel/lucide from CDNs for preview only — in the app use your bundled React and icon set.

*Live prototype: `ui_kits/website/grouped-masthead.html` (open in browser, toggle dark/light at the bottom, scroll to see the settle).*
