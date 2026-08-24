# Handoff: Queenzone mobile app homepage (portal front page)

## Overview
A redesigned **mobile homepage / front page** for the Queenzone app (Queenzone.org — the preserved Queenzone.com archive). The existing home leans historical; this design leads with what is happening *now*: latest news, live forum activity, newest gallery uploads and the member's messages — while keeping the archive's editorial, cinematic character (Cormorant Garamond / Inter / Cinzel, ~90% monochrome, alternating light and rich-black bands, gold used only for anniversary/live markers).

Ships in **light and dark mode**, switchable from a header control.

## About the design files
The file in this bundle (`Queenzone Home.dc.html`) is a **design reference created in HTML** — a prototype showing intended look and behaviour, not production code to copy. The task is to **recreate this screen in the app's existing environment** (React Native / SwiftUI / Kotlin / React web app — whatever the app is built in), using its established components, navigation and theming. If no environment exists yet, pick the most appropriate framework and implement there.

Two things in the HTML are scaffolding only and must NOT be ported:
- `ios-frame.jsx` — a fake iPhone bezel/status bar used to preview the screen at 402×874. The real app has a real status bar.
- The `<x-dc>` / `support.js` prototype runtime.

## Fidelity
**High fidelity.** Colours, type, spacing, radii and copy are final-intent. Recreate pixel-faithfully at 402pt logical width, then let it flow on larger/smaller devices. Content itself is placeholder-real: the news items, dates and lead story are actual queenzone.org content from August 2026; forum threads, messages and "new image" counts are representative and should come from the API.

---

## Screen: Home (front page)

**Purpose:** the member's daily entry point — see what's new across news, forum, photography and their inbox, and get one tap into any of them.

**Frame:** 402pt wide, safe-area aware. Single vertical scroll. Sticky app bar at top, sticky tab bar at bottom.

**Vertical order (top → bottom):**

1. **App bar** (sticky, height 60 + status bar; background `--chrome` with `backdrop-filter: saturate(180%) blur(12px)`; 1px bottom hairline `--line`; padding 10 / 18 / 12)
   - Left: crest 24pt tall (`crest-black.png` in light mode, `crest-white.png` in dark) + wordmark "QUEENZONE" — Cinzel 600, 13pt, letter-spacing 0.18em, uppercase, colour `--txt`.
   - Right: four 38×38 targets, gap 4 — appearance toggle (moon in light / sun in dark), search, messages, avatar.
     - Icons: Lucide-style outline, 20pt, stroke 1.5, colour `--txt` (`#2B2B2B` light / `#F2F1ED` dark).
     - Messages badge: burgundy `#6B1F33` pill, min-width 15, height 15, radius 999, white 9pt/600 label, 1.5px border in `--bg`, positioned top 6 / right 5. Value = unread count (default 3).
     - Avatar: 32×32 circle, `--bg-alt` fill, 1px `--line` border, initials in Cormorant Garamond 600 13pt.
   - Note: hit targets are 38–40pt here for visual density; if your platform's minimum is 44pt, expand the tappable area without changing the visual size.

2. **Live strip** (optional — toggle `showTicker`): background `--band`, padding 9 / 18. Gold `#B89A4A` 6pt dot · "LIVE" in Cinzel 600 10pt / 0.20em uppercase gold · body text Inter 12pt `rgba(255,255,255,0.72)`, single line, ellipsised: "126 members reading · 14 new forum replies today".

3. **Filter chips** (horizontal scroll, no scrollbar; padding 14 / 18 / 12; 1px bottom hairline; gap 8)
   - Labels: All · News · Forum · Photography · Timeline. One active at a time.
   - Chip: padding 7×14, radius 999, Inter 500 12pt, 0.08em uppercase. Inactive = transparent fill, 1px `--line` border, `--chip-off` text. Active = `--chip-on-bg` fill, no border, `--chip-on-txt` text. Background transition 180ms `cubic-bezier(0.22,0.61,0.36,1)`.
   - Behaviour in the real app: filters the feed below (currently visual state only).

4. **Lead story** (full-bleed, height 300)
   - Greyscale image (`filter: grayscale(1)`, opacity 0.86) on `#111111`, plus scrim `linear-gradient(to top, rgba(17,17,17,0.94) 0%, rgba(17,17,17,0.55) 42%, rgba(17,17,17,0.05) 100%)`.
   - Content bottom-anchored, padding 0 20 20: badge "LEAD STORY" (burgundy `#6B1F33`, white Cinzel 600 9.5pt / 0.18em, padding 5×9, radius 2) + timestamp "2 HOURS AGO" (Inter 500 11pt / 0.10em uppercase, `rgba(255,255,255,0.6)`), gap 10.
   - Headline: Cormorant Garamond 500, 29pt / 1.08, letter-spacing −0.015em, white, `text-wrap: pretty`. Copy: "Queen Budapest screening comes to The Roundhouse".
   - Standfirst: Inter 14pt / 1.5, `rgba(255,255,255,0.76)`: "An exclusive screening of the Budapest concert, 3 October 2026."
   - Whole block is one tap target → article view. This band is identical in both themes.

5. **Latest news** (padding 26 / 20 / 8)
   - Header row: "Latest news" (Cormorant 500 23pt, `--txt`) + right link "ALL 4,000+" (Cinzel 600 10pt / 0.16em uppercase, `--lnk`).
   - Three rows, each 1px top hairline `--line`, padding 14 vertical, flex gap 14:
     - Date eyebrow: Cinzel 600 10pt / 0.14em uppercase, `--archive` (royal purple).
     - Title: Cormorant Garamond 600, 18pt / 1.25, `--txt`, `text-wrap: pretty`.
     - Thumbnail: 76×76, radius 3, `object-fit: cover`, greyscale.
   - Last row also carries a bottom hairline. Real copy in the prototype:
     - 14 AUGUST 2026 — "Roger Taylor releases new single and video, 'I See You Now'"
     - 11 AUGUST 2026 — "Queen's last concert with Freddie Mercury, forty years on"
     - 07 AUGUST 2026 — "Brian May joins The Darkness on 'The Legend of Eternia'"

6. **In the forum** (dark band: `--band`, padding 28 / 20 / 26, `margin-top: 26`, `overflow: hidden`)
   - Crest watermark `crest-white.png`, width 168, opacity 0.06, positioned right −46 / top −18.
   - Eyebrow "THE COMMUNITY" (Cinzel 600 10pt / 0.20em, gold), heading "In the forum" (Cormorant 500 23pt, white), right link "ENTER" (Cinzel 600 10pt / 0.16em, `rgba(255,255,255,0.66)`).
   - Three thread rows, 1px `rgba(255,255,255,0.16)` top hairline, padding 14 vertical, gap 12:
     - Avatar: 34×34 circle, fill `rgba(255,255,255,0.10)`, 1px `rgba(255,255,255,0.18)` border, initials Cormorant 600 12pt `rgba(255,255,255,0.85)`.
     - Title: Inter 500 14.5pt / 1.3, white.
     - Meta: Inter 11pt / 0.06em uppercase, `rgba(255,255,255,0.46)` — "Live & Tours · 18 replies · 20 min ago".
     - Unread/new marker on the freshest thread only: 6pt gold dot at row end.
   - Prototype threads: "Was Knebworth really the right ending?" (Live & Tours, 18 replies, 20 min) · "Rockfield sessions: what the tapes actually show" (Recordings, 7 replies, 1 hr) · "Identifying this 1977 News of the World print" (Collectors, 32 replies, 3 hrs).

7. **New in the gallery** (padding 26 / 0 / 4)
   - Header row inset 20: "New in the gallery" + "BROWSE" link (`--lnk`).
   - Horizontal rail, gap 10, inset 20, no scrollbar. Cards 148 wide: 148×148 image (radius 3, greyscale, `--bg-alt` placeholder), then category name (Cormorant 600 14pt, `--txt`, margin-top 9) and count line (Inter 10.5pt / 0.08em uppercase, `--txt3`) — "968 images · 24 new".
   - Prototype categories: Fan Pics (968 · 24 new) · Brian May (670 · 9 new) · Live & Tours (552 · 6 new).

8. **Your messages** (optional — toggle `showMessages`; only for signed-in members)
   - Background `--bg-alt`, padding 26 / 20, `margin-top: 26`, 1px top and bottom hairlines.
   - Header row: "Your messages" + "INBOX" link.
   - Two rows, 1px top hairline, padding 13 vertical, gap 12: 34×34 initials circle (`--pill` fill, 1px `--line`), sender (Inter 600 14.5pt / 1.3, `--txt`), preview (Inter 13pt / 1.35, `--txt2`, single line ellipsised), unread dot 7pt in `--lnk` on unread rows.

9. **On this day** (dark band `--band`, padding 28 / 20 / 30)
   - Eyebrow "ON THIS DAY" (Cinzel 600 10pt / 0.20em, gold), date line "22 AUGUST 1980" (Cinzel 600 12pt / 0.14em, `rgba(255,255,255,0.5)`).
   - Body: Cormorant Garamond 400, 20pt / 1.4, white: "'Another One Bites the Dust' is released — a US number one built on John Deacon's bass riff."
   - Link "VIEW TIMELINE" (Cinzel 600 10pt / 0.18em, gold) + 14×10 gold arrow, gap 8.

10. **Footer note**: padding 22 / 20 / 26, centred, Inter 11pt / 1.6, `--txt3`: "An independent fan archive. Not affiliated with Queen or its representatives."

11. **Tab bar** (sticky bottom, `--chrome` + blur, 1px top hairline, padding 9 / 8 / 30, 5 equal columns, gap 2)
    - Tabs: Home · News · Photos · Forum · You. Icon 21pt outline stroke 1.5 above label (Inter 500 9.5pt / 0.08em uppercase), gap 4.
    - Active: icon + label in `--lnk` (`#244A8F` light / `#8CA9DD` dark). Inactive: `--txt3` (`#8A8A85` light / `rgba(255,255,255,0.45)` dark).
    - Use the platform's native tab bar; match colours and the Cinzel-free, Inter-cased labels.

---

## Interactions & behaviour
- **Appearance toggle** — header moon/sun switches light ⇄ dark instantly for the whole screen. In production, default to the OS setting and let this control override it (persist the choice).
- **Chips** — single-select; selecting one filters the feed sections below. Background transition 180ms.
- **Tab bar** — switches root sections; Home is the default.
- **Cards / rows** — whole row is the tap target. Web hover: images ease from greyscale toward colour, cards lift 3px with a soft shadow, links darken to `#1B3A72`. Touch: 96% opacity press state, no bounce.
- **Motion budget** — fades and gentle reveals only; durations 180 / 320 / 620ms, easing `cubic-bezier(0.22,0.61,0.36,1)`. Respect reduced-motion: drop the greyscale→colour reveal and the chip transition.
- **Live strip** — poll or subscribe for member/reply counts; hide the strip entirely when there is no live activity rather than showing zeros.
- **Loading** — skeletons that mirror the layout: grey blocks for the 300pt hero, 76pt thumbs and 148pt rail cards; hairlines and section headings render immediately.
- **Empty / signed out** — hide "Your messages" for guests and put a quiet "Member sign in" row in its place. If a section has no new content, keep the section but show its latest items without "new" counts.
- **Errors** — per-section inline retry (single line + "RETRY" in Cinzel caps); never block the whole page.
- **Responsive** — content column stays single-column up to ~600pt; above that, the news list and gallery rail can go two-up. The 300pt hero grows to 360pt on tall devices.

## State
- `theme`: 'light' | 'dark' (persisted; initialised from OS).
- `filter`: 'All' | 'News' | 'Forum' | 'Photography' | 'Timeline'.
- `tab`: 'Home' | 'News' | 'Photos' | 'Forum' | 'You'.
- `showTicker`, `showMessages`: booleans (feature flags / auth state in production).
- `unreadCount`: number (drives the header badge).
- Data needed: lead story, latest news (3), active forum threads (3) with reply counts and relative times, gallery categories (3) with totals and new counts, member messages (2) with unread flags, on-this-day entry, live activity counts.

## Design tokens

Theme-scoped (light / dark):

| Token | Light | Dark | Use |
| --- | --- | --- | --- |
| `--bg` | `#FFFFFF` | `#121212` | page surface |
| `--bg-alt` | `#F7F6F3` | `#1A1A1A` | secondary bands, avatars |
| `--band` | `#111111` | `#000000` | dark editorial bands |
| `--desk` | `#E6E4DF` | `#1B1B1A` | prototype surround only |
| `--txt` | `#2B2B2B` | `#F2F1ED` | primary text |
| `--txt2` | `#5F5F5B` | `rgba(255,255,255,0.66)` | secondary text |
| `--txt3` | `#8A8A85` | `rgba(255,255,255,0.45)` | meta / muted |
| `--line` | `#E8E8E8` | `rgba(255,255,255,0.14)` | hairlines |
| `--chrome` | `rgba(255,255,255,0.92)` | `rgba(18,18,18,0.92)` | app bar / tab bar |
| `--lnk` | `#244A8F` | `#8CA9DD` | links, active tab |
| `--archive` | `#5D3A8A` | `#B79BD8` | archive/date eyebrows |
| `--pill` | `#FFFFFF` | `#1F1F1F` | avatar fill on alt surface |

Fixed brand accents (same in both themes): burgundy `#6B1F33` (featured/editorial, unread badge), antique gold `#B89A4A` (live, anniversary, on-this-day — rarest), link hover `#1B3A72`. On dark bands text is white / `rgba(255,255,255,0.66)` / `rgba(255,255,255,0.46)`.

**Typography** — Cormorant Garamond (display: headlines, section headings, on-this-day body, initials) 400–600, tracking −0.015em on large sizes; Inter (body + UI) 11–14.5pt; Cinzel (eyebrows, dates, caps links only — never body) 600, tracking 0.14–0.20em, uppercase.
**Type scale used:** 29 / 23 / 20 / 18 / 14.5 / 14 / 13 / 12 / 11 / 10.5 / 10 / 9.5.
**Spacing:** 4pt base. Section padding 26–28 vertical, 20 horizontal. Row padding 13–14. Gaps 4 / 8 / 9 / 10 / 12 / 14.
**Radii:** 2 (badge) · 3 (images, cards) · 999 (chips, dots, avatars). Nothing else.
**Shadows:** none at rest; soft low hover lift only (`0 6px 18px rgba(17,17,17,0.10)`), 3px translate.
**Blur:** app bar and tab bar only — `saturate(180%) blur(12px)` over 92% opaque surface.

## Assets
- `assets/crest-black.png`, `assets/crest-white.png` — Queen crest, from the Queenzone design system. Header lockup and the 6% forum watermark. Use the app's existing crest asset if one is already bundled.
- Photography in the prototype (`img-stage.jpg`, `img-portrait.jpg`, `img-crowd.jpg`, `img-hero.jpg`) is **placeholder** from the design system; real content comes from the archive CDN. All archive imagery is presented black-and-white (`grayscale(1)`), documentary style, no filters or modern grading.
- Icons: Lucide outline set, stroke 1.5 — search, mail, sun, moon, home, newspaper, image, message, user, arrow-right. Swap for the app's existing icon set if it matches that weight.

## Files
- `Queenzone Home.dc.html` — the design reference (light + dark, interactive: theme toggle, chips, tabs). Open in a browser.
- `assets/` — crest variants used by the header and watermark.
- The prototype loads the Queenzone design system's token stylesheets from `_ds/…/tokens/*.css` (colours, type, spacing); those files were not bundled here — the tables above carry every value the screen uses.
