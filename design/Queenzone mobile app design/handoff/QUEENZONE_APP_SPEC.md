# Queenzone — React Native app build spec

*Version 2 · 22 August 2026.*

Hand this file (plus `theme.ts`) to a coding agent. It is the complete style, component and
screen contract for the Queenzone iOS + Android apps. Visual reference: `Queenzone App.dc.html`
(interactive prototype — toggle iOS/Android in the header).

---

## 0. Product in one paragraph

Queenzone.org publishes a preserved fan archive of Queen material — 4,000+ news articles, 100+
long-form features, tens of thousands of photographs, 100,000+ forum posts. The app must feel like
a **premium music documentary or collector's box set**: editorial, cinematic, monochrome, calm.
Not a fan forum, not a social feed. Voice is third-person and authoritative — museum wall text,
never hype. **No emoji anywhere.** British date style (`13 July 1985`). Roman numerals (Cinzel)
mark historical entries.

**Design decisions already made (do not relitigate):**

| Decision | Value |
|---|---|
| Platform parity | Brand-first, near-identical UI. Only OS *chrome* differs (status bar, nav bar, tab bar, sheets, press feedback, back). |
| Theme | **Dark-first.** `#111111` is the default page surface. Light theme exists for `system` preference. |
| Navigation | Bottom tab bar, 5 tabs, on **both** platforms: **Home · News · Photography · Archive · Forum** — mirroring the website nav. Profile sits behind the avatar in the Home masthead, not in the tab bar. |
| Accounts | Real accounts. Read, save, and full forum posting. |
| Accent on dark | **Antique Gold `#B89A4A`** takes the link/active/CTA role (Royal Blue fails contrast on `#111`). On light, Royal Blue `#244A8F` resumes that role. |

---

## 1. Stack

- React Native (0.7x) + TypeScript, Expo or bare — either is fine.
- **Navigation:** `@react-navigation/native` — `BottomTabNavigator` at the root, one
  `NativeStackNavigator` per tab. Native stack gives iOS edge-swipe back and Android hardware
  back for free.
- **Lists:** `FlatList` / `FlashList` everywhere. Never `ScrollView` + `.map()` for archive data.
- **Images:** `expo-image` (or `react-native-fast-image`) — needs `recyclingKey`, blurhash
  placeholder, and a **saturation-0 colour matrix** (see §2.6).
- **Fonts:** bundled TTFs, loaded via `expo-font` / `react-native.config.js`. Cormorant Garamond,
  Inter, Cinzel. Never fall back to system serif — the layout is tuned to Cormorant's metrics.
- **State:** React Query (or RTK Query) for archive fetches; Zustand/Context for session, theme,
  saved-items and text-size preferences. Persist with MMKV or AsyncStorage.
- **Icons:** `lucide-react-native`, stroke width **1.5**, outline only. The prototype inlines the same
  Lucide v0.446 geometry as React-owned SVG, so the glyphs match one-for-one: `house`, `newspaper`, `camera`, `archive`,
  `message-square` (tabs); `search`, `bookmark`, `share`, `chevron-left/right`, `arrow-left`,
  `x`, `plus`. No filled/duotone glyphs,
  no unicode-as-icon. Icon sizes: 18–20 inline, 24–25 tab bar, 28 section features.
- The crest is **not an icon** — it is a brand asset (`crest-white.png` on dark,
  `crest-black.png` on light, `crest-silver.png` for premium hero moments).

---

## 2. Foundations

All values live in `theme.ts`. Consume via a `useTheme()` hook returning `{ mode, c, type, space, radius, chrome }`
where `c` is `theme.dark` or `theme.light`.

### 2.1 Colour — the 90/10 rule
90% of every screen is monochrome. Accent colour appears only where it carries meaning:

| Role | Dark | Light | Where |
|---|---|---|---|
| `accentPrimary` | `#B89A4A` gold | `#244A8F` blue | Links, active tab, primary CTA, reading progress, save-state fill |
| `accentArchive` | `#5D3A8A` purple | same | Timeline / historical entries |
| `accentEditorial` | `#6B1F33` burgundy | same | Featured / premium story marks |
| `accentSpecial` | `#B89A4A` gold | same | Anniversary badges (rarest token — max one per screen) |

Never use accent for decoration, backgrounds, or more than ~10% of a screen's pixels.

### 2.2 Type
Cormorant Garamond → all titles, standfirsts, pull quotes, drop caps, big numerals.
Inter → body, lists, UI, meta lines. Cinzel → uppercase eyebrows (`letterSpacing: 2.2`) and
Roman numerals **only**. Full scale in `theme.type`.

Meta lines are uppercase, 10.5pt, `letterSpacing: 0.85`: `13 JULY 1985 · 8 MIN READ`.

### 2.3 Spacing & layout
4pt base. Screen gutter **24** (photo grid is the exception: 3px gaps, 3px outer). Section rhythm
34–44. Reading measure: full width minus 26 gutters. Cards on dark carry no border unless they sit
on `surfaceCard`; separators are 1px `hairline`.

### 2.4 Radii & elevation
2 / 3 / 4 only. Pills (`999`) for filter chips and the Android FAB (18). Sheets 20 (Android top
corners) / 14 (iOS card). Shadows soft and low — `shadow.card`, `shadow.lift`, `shadow.fab`.

### 2.5 Motion
`180 / 320 / 620ms`, easing `cubic-bezier(0.22, 0.61, 0.36, 1)`. Screen content fades + rises 6px
on mount (320ms). Images fade in on decode (620ms). No parallax, no looping motion, nothing bouncy.
Wrap every animation in a `useReducedMotion()` check — when reduced, cross-fade only.

### 2.6 Imagery
**All archival photography renders monochrome** — greyscale, contrast 1.05, no filters, no modern
grading. Heroes carry the cinematic scrim from `theme.imagery.scrimBottom` (a 4-stop
`expo-linear-gradient`, stops `[0, 0.32, 0.74, 1]`) so type sits on the bottom third.
The crest may appear as a watermark at 5–7% opacity behind dark feature blocks. No decorative
gradients anywhere else.

---

## 3. Component contracts

Build these as the app's primitives. Props listed are the full public surface; every one is
exercised somewhere in §4.

### `<Eyebrow>`
`{ children: string; tone?: 'accent' | 'muted' | 'onDark' }` — Cinzel 10pt, tracking 2.2,
uppercase. Default tone `accent`. No press state.

### `<MetaLine>`
`{ parts: string[]; tone?: 'muted' | 'onDark' }` — joins with ` · `, uppercase 10.5pt Inter Medium.

### `<Button>`
`{ variant: 'primary' | 'outline' | 'ghost'; size?: 'md' | 'sm'; label: string; onPress; disabled?; loading?; accessibilityLabel? }`
- `primary` — `accentPrimary` fill, `textOnAccent` label, radius 2, height 48 (md) / 40 (sm).
- `outline` — transparent, 1px `borderStrong`, label `textPrimary`.
- `ghost` — no border, label `accentPrimary`.
- Label: Inter Medium 12, `letterSpacing: 1.2`, uppercase.
- States: press → `translateY(1)` + `accentPress` (iOS: opacity 0.85; Android: ripple
  `accentTintWeak`). Disabled → 40% opacity, no feedback. Loading → 16px spinner replaces label,
  width held.

### `<IconButton>`
`{ icon: LucideIcon; onPress; accessibilityLabel: string (required); tone?: 'onDark' | 'accent'; size?: 20 | 24 }`
Hit area **44×44 minimum** on both platforms. Android gets a 22-radius ripple; iOS opacity 0.6.

### `<Chip>` (filter)
`{ label: string; active: boolean; onPress }` — pill, height 34, padding 8/15, Inter Medium 11
uppercase tracking 1.1. Active: `accentPrimary` fill + `textOnAccent`. Inactive: transparent +
1px `border`. Rendered in a horizontal `FlatList` with `contentContainerStyle` gutter 24.

### `<Badge>`
`{ label: string; role: 'restored' | 'anniversary' | 'featured' | 'archive' | 'community' }`
Maps role → colour: restored/anniversary `accentSpecial`, featured `accentEditorial`,
archive `accentArchive`, community `textSecondary`. Renders as an Eyebrow, not a filled pill.

### `<ArticleRow>`
`{ item: { id, title, kicker, kickerRole, meta, thumbUri }; onPress }`
92×92 monochrome thumb (radius 2) + column: Badge, Cormorant 20/23.5 title (2-line clamp),
MetaLine. Row padding 16/24, 1px top hairline. Press: background `rgba(255,255,255,0.04)`
(iOS) / ripple (Android). Used by News, Search, related lists.

### `<FeatureCard>` (horizontal rail)
`{ item: { id, title, kicker, meta, imageUri }; onPress; width?: 216 }`
216×150 image (radius 2), Eyebrow, Cormorant 21 title (3-line clamp), MetaLine. Rail is a
horizontal `FlatList`, `snapToInterval: 230`, `decelerationRate: 'fast'`.

### `<HeroFeature>`
`{ item: { title, standfirst, meta, imageUri, kicker }; onPress; height?: 468 }`
Full-bleed monochrome image, 4-stop scrim, bottom-anchored Eyebrow → Cormorant 38 title →
standfirst 15/23 → MetaLine. Whole surface is one pressable with
`accessibilityRole="button"`.

### `<PhotoTile>`
`{ uri: string; index: number; onPress }` — `aspectRatio: 1`, 3px grid gap, `recyclingKey={uri}`.
Grid = `FlatList numColumns={3}`, `getItemLayout` supplied.

### `<ThreadRow>`
`{ item: { id, title, authorInitial, board, author, replies, lastPostAt }; onPress }`
36px circular avatar (initial, `surfaceCard` bg, 1px border) + title (Inter Medium 15.5,
2-line clamp) + MetaLine (`author · board`) + right-aligned reply count.

### `<PostBlock>`
`{ post: { author, authorInitial, when, body, edited? }; onQuote?; onReport? }`
34px avatar row → author 13.5 Medium → `when` in Cinzel-cased meta → body Inter 15.5/26.
Long-press opens the post action sheet.

### `<CrestSeal>`
`{ variant: 'white' | 'black' | 'silver' | 'lineart'; height: number; opacity?: number }`
Used as footer seal (h 30–38, opacity 0.3) and section watermark (h 150, opacity 0.06).

### `<AppBar>`
`{ title?: string; showBack?: boolean; actions?: ReactNode; translucent?: boolean }`
Renders platform chrome from `theme.chrome[Platform.OS]` — see §5. Prefer configuring
`@react-navigation` `headerLeft/headerTitle/headerRight` over a hand-rolled bar.

### `<Sheet>`
`{ open: boolean; onClose; title: string; items: { label, onPress, danger? }[] }`
iOS → floating card (radius 14, 10px side inset, 14px bottom inset) with centred 17pt rows and a
**separate Cancel card** below. Android → edge-to-edge bottom sheet (radius 20 top, drag handle,
left-aligned 16pt rows, **no** Cancel). Both: scrim `rgba(0,0,0,0.5)`, enter 280ms slide-up,
scrim tap closes, `accessibilityViewIsModal`.

### `<Switch>`
Use the platform `Switch` with `trackColor.true = accentPrimary`. Do not restyle further.

---

## 4. Screens

Navigator shape:

```
RootTabs
├── HomeStack      → Home, Story, Search, Profile, SavedList, Settings, Auth(modal)
├── NewsStack      → NewsIndex, Story
├── PhotosStack    → PhotoIndex, PhotoViewer
├── ArchiveStack   → ArchiveHub → Stories · Timeline · Biography · Discography
│                                 · Tribute · FanPerformances · RecentlyRestored · AboutTheArchive
└── ForumStack     → ForumIndex, Thread, Composer(modal)
```

**Why these five.** The tab bar mirrors the website's own nav (News · Stories · Photography ·
Forum · Timeline) rather than inventing an app-only IA. Stories and Timeline are two of eight
archive destinations, so they live under **Archive** — a hub screen, not a dead end — which also
carries the Biography, Discography, the Freddie Mercury tribute, Fan performances, Recently
restored and About the archive. **Home** is the curated front page (the site's homepage), not a
personalised feed; it is called Home, never "Today". **Profile & settings** is reached from the
avatar in the Home masthead: it is a destination users visit rarely and does not deserve a
permanent fifth of the tab bar.
Detail screens (`Story`, `PhotoViewer`, `Thread`, `Search`, `Profile`) **hide the tab bar**
(`tabBarStyle: { display: 'none' }` on those routes). Every stack resets to its root on tab
re-press (`navigation.popToTop()`).

### 4.1 Home (tab 1) — no app bar, hero starts under the status bar
Sections, in order:
0. **Floating masthead** over the hero: crest + `QUEENZONE` wordmark (Cinzel 13, tracking 0.18em)
   left; search icon + 32px avatar right. The avatar pushes Profile. No solid bar — it sits on the
   hero scrim, respecting the safe-area inset.
1. `HeroFeature` (468) — the day's lead. Taps → Story.
2. **Featured stories** — section header (Cinzel 11 + ghost "All" → News) then `FeatureCard` rail.
3. **This day in Queen history** — `surfaceRaised` block (`#181614`), 1px `rgba(184,154,74,0.34)`
   border, crest watermark, Cinzel Roman numeral, 15/24 body, outline Button "Read the entry".
4. **Explore the archive** — 4 chevron rows (Cormorant 20 title + count meta) linking into the
   Archive hub, ghost "All" → Archive. This is how Timeline, Biography, Discography and the
   Tribute surface on the front page.
5. **Popular discussions** — 3 `ThreadRow`s, ghost "Forum" link.
6. Footer — `CrestSeal` (h 38, opacity 0.34) + disclaimer: *"An independent fan archive. Not
   affiliated with Queen or its representatives."*

Pull-to-refresh refetches the lead + On This Day. Status-bar content is always `light-content`
in dark theme. Implement as a single `FlatList` with section rows, not nested ScrollViews.

### 4.2 News index (tab 2)
Page title block (Eyebrow + Cormorant 34 + count line "4,127 articles · restored from
Queenzone.com"), sticky decade `Chip` row (ALL / 1970s / 1980s / 1990s / 2000s), then
`ArticleRow` list. Infinite scroll — 20 per page, footer outline Button "Load older articles" as
the fallback when auto-paging is off. Empty state: crest seal + "No articles for this decade yet."

### 4.3 Story reader (pushed)
- App bar: back + save (Lucide `bookmark`, fills `accentPrimary` when saved) + share. iOS bar is
  translucent-blur; Android elevates on scroll.
- **2px reading-progress bar** directly under the app bar, width = scroll fraction, `accentPrimary`.
- 300px monochrome hero → scrim → title block pulled up 58px over it: Eyebrow, Cormorant 36,
  standfirst 18/29, meta row between hairlines.
- Body: Inter 18/31, 26 gutters, 20 paragraph spacing. **Drop cap** = Cormorant 62 on the first
  paragraph (`float` is unavailable in RN — render the first paragraph as a `<View flexDirection="row">`
  with the cap as its own `<Text>` and the remaining copy in a flexed `<Text>`, or use the
  first-line-inset trick with `textIndent` padding).
- Pull quote: 2px left rule `accentPrimary`, Cormorant 26/33, 28 vertical margin.
- "From the same day" related card → Photos.
- Footer seal + `THE QUEENZONE.COM ARCHIVE` eyebrow.
- Respect a user text-size setting (S/M/L/XL → multiplies `type.longform` only); cap
  `allowFontScaling` growth at 1.4 for titles.

### 4.3b Archive hub (tab 4)
The screen that makes the whole archive reachable — and the one to extend when a new archive
section is commissioned.

Title block: Eyebrow `THE QUEENZONE.COM ARCHIVE`, Cormorant 34 "Explore the archive", one
sentence of scale copy. Then eight `ArticleRow`-shaped destination rows (84px monochrome thumb,
Badge kicker, Cormorant 23 title, count meta, chevron):

| Destination | Kicker / role | Meta | Pushes |
|---|---|---|---|
| Stories | `LONG-FORM` / gold | 104 features · Editorial | StoriesIndex → Story |
| Timeline | `HISTORY` / purple | 1970 — 1991 · 480 entries | Timeline (decade scroller) |
| Biography | `THE BAND` / muted | Nine chapters | Biography → Chapter |
| Discography | `RECORDS` / muted | 15 studio albums · Sleeves & tracklists | Discography → Album |
| Freddie Mercury — a tribute | `IN MEMORIAM` / rose | 1946 — 1991 · Members' memories | Tribute |
| Fan performances | `COMMUNITY` / muted | 212 submissions · Video & audio | FanPerformances |
| Recently restored | `PRESERVED` / gold | 1,240 photographs · 340 articles | Photos tab |
| Queenzone.com, preserved | `THE OLD SITE` / muted | How the archive was rebuilt | Story |

Footer: crest seal + disclaimer. Search action in the app bar.

**The destination screens themselves are not yet designed.** Timeline, Biography, Discography and
the Gallery have existing web templates in the design system
(`templates/biography`, `templates/discography`, `templates/gallery`) — port those layouts to
the mobile shapes in `STYLE_GUIDE.md` §3 rather than inventing new ones. Tribute and Fan
performances are new and need design before build.

### 4.4 Photography (tab 3)
Title block + category `Chip`s (ALL / LIVE / STUDIO / PORTRAITS / BACKSTAGE) + 3-up `PhotoTile`
grid, 3px gutters. Page footer shows `PAGE 1 OF 104` in Cinzel. Tapping a tile pushes the viewer
with a shared-element fade (no zoom-bounce).

### 4.5 Photo viewer (pushed, immersive)
No app bar, no tab bar, `#000` background. Top row: close (X), `n of 1,240` in Cinzel, save +
share. Image `resizeMode="contain"`, pinch-zoom + double-tap zoom, horizontal swipe = prev/next
(also 44px chevron buttons for accessibility). Bottom: Cormorant 21 caption + MetaLine
(date · restoration credit). Single tap toggles the chrome.

### 4.6 Forum (tab 4)
Masthead: Eyebrow "Community", Cormorant 34 "Forum", three stat columns
(104,882 posts · 6,410 members · 18 boards) in Cormorant 22 over Cinzel-cased labels.
"Recent threads" `ThreadRow` list. Compose entry point is the one real platform difference:
**iOS** — "New" text button in the nav bar right slot. **Android** — 58dp gold FAB, bottom-right,
18 radius, `Plus` icon in `textOnAccent`. Both open the same `Sheet` → Composer modal.

### 4.7 Thread (pushed)
Header block: board Eyebrow, Cormorant 28 title (no clamp), MetaLine
(`148 replies · Last post 2 hours ago`). `PostBlock` list, hairline separated. Sticky bottom
reply bar (56 tall, `surfaceRaised`, 1px top hairline): avatar + "Add to the discussion" field →
opens Composer. Signed-out users see an outline Button "Sign in to reply".

### 4.8 Search (pushed from any tab's app bar)
Field: 44 tall, `#1D1D1D`, 1px border, search glyph, placeholder
*"Search 4,000+ articles and photographs"*. iOS radius 10; Android pill. Autofocus on mount.
Section label "Suggested" (recent + curated) switches to "Results" once `query.length > 1`.
Result rows show title + typed tag (`Story · 8 min read`, `Gallery`, `News · 13 July 1985`,
`Forum · 312 replies`) with a chevron; tag colour is `accentPrimary` for editorial types,
`textMuted` otherwise. Debounce 250ms. Empty: "Nothing in the archive matches that — yet."

### 4.9 Profile & settings (pushed from the Home masthead avatar)
Avatar (62, Cormorant initials) + handle + `MEMBER SINCE 2004 · 1,208 POSTS` in
`accentPrimary`. Two-up stat grid (saved articles / saved photographs) split by 1px hairlines.
**Library** rows: Saved articles · Saved photographs · Downloaded for offline · Reading history.
**Settings** rows: *On This Day notification* (Switch, subtitle "One entry each morning") ·
*Appearance* (Dark · follows system) · Text size · Notifications · Account & privacy ·
About the archive. Footer: crest seal + disclaimer. Signed-out variant replaces the header with
a crest seal, one line of copy, and primary "Sign in" + ghost "Create an account".

---

## 5. Platform chrome — the complete diff

Everything not in this table is identical on both platforms.

| | iOS | Android |
|---|---|---|
| Status bar | 47pt inset, Dynamic Island cutout | 32dp, edge-to-edge, no cutout |
| Nav bar | 44pt, **centred** 17pt semibold title | 56dp, **left-aligned** 20dp title |
| Back | Chevron + "Back" label, `accentPrimary`; edge-swipe gesture | Arrow glyph only, `textPrimary`; hardware/gesture back |
| Tab bar | 83pt (incl. 34pt home indicator), 25pt icon + 10pt label, active = gold tint | 80dp, 24dp icon inside a **gold-tinted 64×32 pill**, 11.5dp label |
| Long tab labels | "Photography" truncates — allow 2 lines or abbreviate to "Photos" at ≤375pt width | same rule at ≤360dp |
| Home affordance | 134×5 indicator pill drawn at the bottom | none (gesture inset only) |
| Sheets | Action sheet: floating card + separate Cancel, centred rows | Bottom sheet: edge-to-edge, drag handle, left rows, no Cancel |
| Compose | Nav-bar "New" text button | 58dp FAB |
| Press feedback | Opacity 0.85 / 0.6, `translateY(1)` on buttons | `android_ripple` at `accentTintWeak`, borderless for icons |
| Search field | Radius 10 | Pill radius |
| Switch | iOS switch, gold track | Material switch, gold track, dark thumb |
| Haptics | `selectionAsync` on save/tab change | none (system default) |

Implementation: one `chrome = theme.chrome[Platform.OS]` object; branch on
`chrome.tabActiveStyle`, `chrome.sheet`, `chrome.pressFeedback`, `chrome.backAffordance`.
No `Platform.select` scattered through screen files.

---

## 6. Accessibility

- **Contrast:** gold `#B89A4A` on `#111111` = 6.4:1 (passes AA for text and UI). Never place gold
  text on `surfaceCard` lighter than `#1E1E1E`. `textMuted` (50% white) is for ≥12pt only —
  never for the sole copy of an interactive label.
- **Touch targets:** 44×44 minimum (iOS HIG) / 48×48 preferred (Material). Chips are 34 tall but
  carry `hitSlop: { top: 7, bottom: 7 }`.
- **Font scaling:** `allowFontScaling` on for all body copy; clamp display titles with
  `maxFontSizeMultiplier: 1.4`, meta lines 1.6. Test at 200% — the Today hero must reflow, not clip.
- **Labels:** every `IconButton` requires `accessibilityLabel`. Images require descriptive
  `accessibilityLabel` from the archive caption (never "image"). Decorative crests get
  `accessibilityElementsHidden` / `importantForAccessibility="no"`.
- **Roles & state:** pressable rows `accessibilityRole="button"`; chips
  `accessibilityRole="button"` + `accessibilityState={{ selected }}`; tabs get
  `accessibilityRole="tab"`. Save button announces "Saved" / "Not saved".
- **Reading order:** group card content with `accessible={true}` on the row so VoiceOver/TalkBack
  reads "kicker, title, meta" as one item, not four.
- **Reduced motion:** `useReducedMotion()` → drop the 6px rise, keep opacity cross-fades.
- **Screen titles:** set `accessibilityLabel` on the AppBar title so navigation announces the
  screen name on push.

---

## 7. React Native gotchas for this design

1. **No `float`** — the drop cap needs the row-layout workaround in §4.3.
2. **No `text-wrap: pretty`** — set `numberOfLines` clamps deliberately (titles 2–3 lines) and
   accept RN's line breaking.
3. **No CSS `filter`** — greyscale comes from `expo-image`'s
   `tintColor`-free colour matrix, `@react-native-community/image-filter-kit`, or pre-processed
   monochrome derivatives served by the CDN. Decide once; the whole archive depends on it.
4. **`letterSpacing` is in points, not em** — the Cinzel eyebrow value is `2.2` at 10pt.
5. **Cormorant sits high in its box** — expect to nudge `lineHeight` and add
   `includeFontPadding: false` on Android for every display `Text`.
6. **`backdrop-filter` has no RN equivalent** — translucent bars use
   `expo-blur`'s `<BlurView tint="dark" intensity={40}>`; fall back to `surfaceBarBlur` (90%
   opaque) where BlurView is expensive.
7. **`gap` works in RN 0.71+** — use it; do not add per-child margins.
8. **`aspectRatio: 1` tiles** need `getItemLayout` on the 3-column grid or scrolling 1,240 photos
   stutters.
9. **Sticky headers:** `stickyHeaderIndices` on `FlatList` for the chip rows; Android needs
   `removeClippedSubviews={false}` to avoid the chips vanishing.
10. **Shadows differ** — iOS uses `shadowOffset/Radius/Opacity`, Android only `elevation`.
    `theme.shadow` sets both; never hand-roll.
11. **SafeArea:** `react-native-safe-area-context` — `useSafeAreaInsets()` on every immersive
    screen (Today hero, Photo viewer) rather than `SafeAreaView`, so the hero can bleed under the
    status bar while the text respects the inset.

---

## 8. Definition of done

- [ ] Fonts bundled; no system-serif fallback visible anywhere.
- [ ] Dark theme default; light theme reachable via system preference with the blue accent role.
- [ ] All photography renders monochrome, one consistent pipeline.
- [ ] Accent coverage on any screen ≤ ~10% of pixels; at most one gold badge per screen.
- [ ] Tab bar, nav bar, sheets, FAB and press feedback match §5 per platform.
- [ ] 44/48pt targets, `accessibilityLabel` on every icon button, 200% text scale tested.
- [ ] No emoji, no clickbait copy, British dates, disclaimer in every footer.
