# Queenzone — mobile app handoff pack

Everything needed to build the Queenzone iOS + Android apps in React Native, in the archive's
visual language. Give this whole folder to your coding agent.

## Read in this order

| File | What it is |
|---|---|
| **`STYLE_GUIDE.md`** | **Read first, and re-read before every new screen or section.** The rules and recipes that keep new work consistent: the five rules, section anatomy, the six section shapes, spacing rhythm, colour-by-meaning table, copy rules, review checklist. |
| `theme.ts` | Drop-in token file: palette, dark + light themes, font families, type scale, spacing, radii, shadows, motion, per-platform chrome metrics, imagery rules. Import it; never hard-code a value it already names. |
| `QUEENZONE_APP_SPEC.md` | The build spec: stack, navigator shape, component contracts (props + states), screen-by-screen notes for all nine screens, the complete iOS/Android chrome diff, accessibility floor, RN gotchas, definition of done. |
| `COMPONENT_RECIPES.tsx` | Working starting code for the primitives (Eyebrow, MetaLine, Button, IconButton, Chip, Badge, ArchiveImage, SectionHeader, HeroFeature, ArticleRow, FeatureRail, FeatureBlock) plus an assembled example screen. |
| `screens/` | Renders of the approved design — iOS Today, iOS story reader, Android Today, Android forum. |
| `assets/` | Crest variants and the monochrome placeholder photography used in the prototype. |
| `prototype/` | The interactive prototype source (`Queenzone App.dc.html`). Opens inside the Queenzone design project; the spec and screens are the portable reference. |

## Suggested prompt to your agent

> Build the Queenzone React Native app from this handoff pack. Read `STYLE_GUIDE.md` and
> `QUEENZONE_APP_SPEC.md` in full before writing code. Use `theme.ts` as the single source of
> design values — do not introduce colours, sizes, radii or fonts it does not define. Start with
> the primitives in `COMPONENT_RECIPES.tsx`, then the tab navigator, then Today → News → Story →
> Photography → Photo viewer → Forum → Thread → Search → Profile in that order. Content is
> identical on iOS and Android; only the chrome in §5 of the spec may differ. Before finishing any
> screen, run the checklist in `STYLE_GUIDE.md` §10.

## The three decisions to make on day one

1. **Greyscale pipeline.** All archive photography renders monochrome and RN has no CSS filter.
   Choose a colour-matrix library, an image-filter package, or pre-processed monochrome
   derivatives from the CDN — then put it behind `ArchiveImage` and nowhere else.
2. **Accent role by theme.** On dark, Antique Gold `#B89A4A` is the link/active/CTA colour
   (Royal Blue `#244A8F` fails contrast on `#111111`). On light, Royal Blue resumes that role.
   Both are already wired in `theme.ts`.
3. **Fonts.** Bundle Cormorant Garamond, Inter and Cinzel as app assets. The layout is tuned to
   Cormorant's metrics — a system-serif fallback will look broken, not merely different.

## Non-negotiables

- No emoji, anywhere.
- Accent colour ≤ ~10% of any screen, and only where it carries one of the four meanings.
- All archival photography monochrome.
- British dates (`13 July 1985`); Cinzel for uppercase eyebrows and Roman numerals only.
- Every footer carries: *"An independent fan archive. Not affiliated with Queen or its
  representatives."*
