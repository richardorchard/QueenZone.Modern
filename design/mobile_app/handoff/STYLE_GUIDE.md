# Queenzone — Style Guide

**This is the document to read before writing any new screen or section.** It is not a list of
tokens (that's `theme.ts`); it is the set of rules and recipes that make a *new* piece of the app
look like it was always there.

Read `QUEENZONE_APP_SPEC.md` for what the nine designed screens contain. Read this for how to
build the tenth.

---

## 1. The five rules

Everything else in this document follows from these. If a new section breaks one, it is wrong even
if it looks nice.

1. **Monochrome carries the page; accent carries meaning.** ~90% of every screen is
   black/white/grey. Gold (`accentPrimary` on dark) appears only on: the active tab, links, one
   primary action, reading progress, save state, and anniversary/restoration marks. If you can't
   name the *meaning* of a coloured pixel, make it monochrome.
2. **Type is the hierarchy, not boxes.** Rank things by typeface and size, not by nesting them in
   cards, tinted panels or borders. Cormorant Garamond descending (38 → 34 → 28 → 21) does the
   work that a card would do elsewhere.
3. **Three typefaces, three jobs, no crossover.** Cormorant = titles and quotes. Inter = body,
   lists, UI. Cinzel = uppercase eyebrows and Roman numerals *only*. Cinzel in a sentence is the
   single most damaging mistake available in this system.
4. **Photography is monochrome, large, and alone.** One strong image per section, never a mosaic
   of small colour thumbs. Never place competing elements over an image except the scrim + title
   block.
5. **Quiet motion, quiet surfaces.** 180/320/620ms, one easing curve, fades and 6px rises. No
   bounce, no parallax, no looping, no decorative gradients, no heavy shadows, no emoji — ever.

---

## 2. The anatomy of a section

Every section in this app is built from the same four parts, in this order. Use them and a new
section will match on the first try.

```
[ EYEBROW ]          Cinzel 10, tracking 2.2, uppercase, accentPrimary or textSecondary
[ TITLE ]            Cormorant 21–38, letterSpacing -0.2 to -0.6
[ BODY / CONTENT ]   Inter 15–18, or a list, or media
[ META ]             Inter Medium 10.5, uppercase, tracking 0.85, textMuted
```

**Section header pattern** (used on Today, and correct for any new rail or list):

```
Eyebrow (Cinzel 11, textPrimary)  ......................  ghost link (Inter Medium 12, accent)
────────────────────────────────────────────────────────  1px hairline, 12 below
```
Gutter 24. Space above a new section: 34. Space between header and content: 14–18.

**Naming.** Section titles follow the archive's own vocabulary: *Hero Feature · Explore the
Archive · Featured Stories · Featured Photography · This Day in Queen History · Popular
Discussions · Recently Restored · Timeline Highlights · From the Vaults*. A new section should
sound like it belongs in that list — "Recently Digitised", "Members' Picks", "The 1986 Tour Book" —
not "Trending", "For You", "Top 10", "Don't Miss".

---

## 3. Choosing a section shape

Six shapes cover everything this app needs. Pick one; do not invent a seventh without reason.

| Shape | When to use | Recipe |
|---|---|---|
| **Hero** | The one most important item on a screen | Full-bleed monochrome image 300–468 tall, 4-stop scrim, bottom-anchored Eyebrow → Cormorant 36–38 → standfirst 15/23 → MetaLine. One pressable. |
| **Rail** | 3–8 peer items, browsable, image-led | Horizontal `FlatList` of 216-wide `FeatureCard`s, `snapToInterval: 230`, gutter 24, image 216×150 radius 2. |
| **List** | Ordered or paginated archive content | `ArticleRow` / `ThreadRow`, 16/24 padding, 1px top hairline per row, 92px thumb or 36px avatar. Never a card wall. |
| **Feature block** | A single editorial statement (On This Day, an appeal, an anniversary) | `surfaceRaised` panel `#181614`, 1px `rgba(184,154,74,0.34)`, crest watermark at 6%, Eyebrow → Cinzel numeral or Cormorant title → 15/24 body → outline Button. Max **one per screen**. |
| **Grid** | Photography only | 3-up, 3px gaps, `aspectRatio: 1`, no captions in the grid. |
| **Stat row** | Archive scale, member counts | 2–3 columns, Cormorant 22–26 figure over a Cinzel-cased 9.5 label. Numbers stated plainly: `104,882`, `4,000+`. |

**Rule of one:** at most one Hero, one Feature block and one Grid per screen. A screen that needs
two Feature blocks actually needs a new screen.

---

## 4. Spacing, measure and rhythm

- **Gutter 24** everywhere. Exceptions: long-form body 26, photo grid 3.
- **Vertical rhythm:** 34 above a section header, 14–18 header→content, 44 before a footer.
- **Row padding** 16 vertical / 24 horizontal; separators are 1px `hairline`, never 2px, never a
  gap-plus-shadow.
- **Line clamps:** card titles 3 lines, list titles 2, standfirsts 3. Set them explicitly —
  ragged clipping is worse than a shorter title.
- **Alternating rhythm.** The web archive alternates dark and light bands; the app is dark
  throughout, so the equivalent rhythm comes from **surface steps**:
  `#111111` page → `#161616` raised → `#1A1A1A` card → `#181614` gold-bordered feature block.
  Use no more than three steps on one screen.

---

## 5. Colour decision table

Ask "what does this mean?", then look it up. Do not pick by taste.

| Meaning | Token | Example |
|---|---|---|
| Interactive / active / progress | `accentPrimary` (gold on dark, blue on light) | active tab, links, save-filled bookmark, reading bar |
| Anniversary, restoration, "on this day" | `accentSpecial` (gold) | `RESTORED`, `ANNIVERSARY` eyebrows, the On This Day block |
| Featured / premium editorial | `accentEditorial` (burgundy) | a curated collection mark |
| History / timeline / archive depth | `accentArchive` (purple) | timeline markers, decade navigation |
| Everything else | monochrome | all body copy, all meta, all borders, all icons |
| Destructive | `danger` | "Delete post", report confirmations |

**Never:** accent-coloured backgrounds for whole sections, two accents in one section, gold text on
any surface lighter than `#1E1E1E`, or accent used to indicate mere newness.

---

## 6. Copy rules for new sections

The voice is a well-edited music documentary: knowledgeable, respectful, third-person.

- **Eyebrows:** 1–4 words, uppercase, no punctuation — `FROM THE VAULTS`, `ON THIS DAY`,
  `RECENTLY RESTORED`.
- **Titles:** sentence case, declarative, specific. *"The day Queen stole Live Aid"*, not
  *"Queen's LEGENDARY Live Aid moment!"*.
- **Standfirsts:** one sentence, ≤ 22 words, adds a fact the title didn't carry.
- **Meta:** `13 JULY 1985 · 8 MIN READ` — British dates, ` · ` separators, uppercase.
- **Numbers:** stated plainly and proudly — `4,000+ articles`, `104,882 posts`.
- **Invitations** are the only place the reader is addressed: *"Explore the archive"*,
  *"Browse the gallery"*.
- **Empty states** are calm and factual: *"No articles for this decade yet."* Never apologetic,
  never jokey.
- **Buttons** are uppercase Inter Medium 12, tracking 1.2, verb-first: `READ THE ENTRY`,
  `OPEN GALLERY`, `LOAD OLDER ARTICLES`.
- **Banned:** clickbait, exclamation marks, "amazing/iconic/legendary" as filler, first-person
  gushing, emoji, ALL-CAPS shouting outside eyebrows and buttons.
- **Every footer** carries: *"An independent fan archive. Not affiliated with Queen or its
  representatives."*

---

## 7. Platform behaviour for new screens

Content is identical on iOS and Android. Only chrome differs, and chrome comes from
`theme.chrome[Platform.OS]` — never from a `Platform.select` written inside a screen file.

When adding a screen, decide only these four things:

1. **Is it a tab root or a pushed detail?** Pushed details hide the tab bar
   (`tabBarStyle: { display: 'none' }`) and show a back affordance (iOS chevron + "Back";
   Android arrow).
2. **Does it need an app bar?** Immersive screens (hero-led roots, media viewers) skip it and use
   `useSafeAreaInsets()` so imagery bleeds under the status bar while text respects the inset.
3. **What are its bar actions?** Max two, right-aligned, `IconButton` with a required
   `accessibilityLabel` and a 44×44 hit area.
4. **Does it create content?** If yes: iOS gets a nav-bar text button; Android gets the 58dp gold
   FAB. Both open the same sheet.

Press feedback: `chrome.pressFeedback` — iOS opacity 0.85 + 1px depress, Android ripple at
`accentTintWeak`.

---

## 8. Accessibility floor (non-negotiable on new work)

- 44×44 minimum hit area (48 preferred on Android); chips use `hitSlop`.
- `accessibilityLabel` on every icon button; archive captions as image labels; decorative crests
  hidden from the a11y tree.
- Group card rows with `accessible={true}` so the eyebrow/title/meta read as one item.
- `allowFontScaling` on for body; `maxFontSizeMultiplier: 1.4` on display titles. Test at 200% —
  sections reflow, never clip.
- `useReducedMotion()` → drop the 6px rise, keep the cross-fade.
- Gold on `#111111` is 6.4:1 — fine for text and UI. 50%-white `textMuted` is decoration and
  secondary meta only, never the sole label of a control.

---

## 9. Worked example — adding a "Recently Restored" section to Today

The reasoning, so the next one can be done without asking.

1. **Meaning:** peer items, image-led, browsable → **Rail** (§3).
2. **Placement:** after "From the vaults", before the On This Day block — Today already has one
   Feature block, so no second one.
3. **Header:** Eyebrow `RECENTLY RESTORED` (Cinzel 11, `textPrimary`) + ghost link `ALL` → Photos,
   hairline under, 34 above.
4. **Items:** `FeatureCard` 216 wide; kicker uses `Badge role="restored"` → gold, because
   restoration is one of the four accent meanings.
5. **Copy:** titles sentence case; meta `RESTORED AUGUST 2026 · 14 FRAMES`.
6. **Motion:** rail inherits the section fade; no per-card animation.
7. **A11y:** each card `accessible`, label = `"${kicker}. ${title}. ${meta}"`.

Result: no new tokens, no new component, no new colour — a new section that is indistinguishable
in kind from the ones already shipped. That is the goal every time.

---

## 10. Review checklist

Before calling a new section done:

- [ ] Eyebrow → Title → Content → Meta order intact, in the right three typefaces.
- [ ] Gutter 24, rhythm 34/14/44, 1px hairlines.
- [ ] Accent used ≤ once for a nameable meaning; no accent backgrounds.
- [ ] Photography monochrome; one strong image, not many small ones.
- [ ] Copy: sentence-case title, British date meta, no hype, no emoji.
- [ ] Shape is one of the six in §3; rule of one respected.
- [ ] Chrome from `theme.chrome`, not inline `Platform.select`.
- [ ] Hit areas, labels, font scaling, reduced motion.
- [ ] Nothing new invented that `theme.ts` already names.
