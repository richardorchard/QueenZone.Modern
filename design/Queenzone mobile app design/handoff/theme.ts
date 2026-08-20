/**
 * Queenzone — React Native theme
 * Generated from the Queenzone Design System (dark-first mobile app).
 * ~90% monochrome. Accent colour carries MEANING, never decoration.
 * Gold is the rarest token: anniversaries, active state, editorial marks.
 */

export const palette = {
  // Monochrome foundation (light surfaces — used by the light theme only)
  white: '#FFFFFF',
  warmWhite: '#F7F6F3',
  greyLight: '#E8E8E8',
  charcoal: '#2B2B2B',
  black: '#111111',

  grey50: '#FBFBFA',
  grey100: '#F2F1ED',
  grey200: '#E8E8E8',
  grey300: '#D6D6D2',
  grey400: '#B4B4AF',
  grey500: '#8A8A85',
  grey600: '#5F5F5B',
  grey700: '#3D3D3B',
  grey800: '#2B2B2B',
  grey900: '#1A1A1A',

  // Accents
  blue: '#244A8F',
  blueDeep: '#1B3A72',
  purple: '#5D3A8A',
  purpleDeep: '#492C6E',
  burgundy: '#6B1F33',
  burgundyDeep: '#551828',
  gold: '#B89A4A',
  goldDeep: '#9C8038',
  goldLift: '#D3B868',
} as const;

/** Dark theme — the app default. */
export const dark = {
  surfacePage: '#111111',
  surfaceRaised: '#161616',
  surfaceCard: '#1A1A1A',
  surfaceSheet: '#1E1E1E',
  surfaceScrim: 'rgba(0,0,0,0.50)',
  surfaceBarBlur: 'rgba(17,17,17,0.90)',

  textPrimary: '#FFFFFF',
  textSecondary: 'rgba(255,255,255,0.66)',
  textMuted: 'rgba(255,255,255,0.50)',
  textOnAccent: '#111111',

  hairline: 'rgba(255,255,255,0.12)',
  border: 'rgba(255,255,255,0.16)',
  borderStrong: 'rgba(255,255,255,0.28)',

  /** Accent roles — on dark, GOLD replaces Royal Blue for links/active state (contrast). */
  accentPrimary: palette.gold,       // active nav, links, primary action, progress
  accentPress: palette.goldDeep,
  accentTintWeak: 'rgba(184,154,74,0.20)', // Material 3 active tab pill
  accentArchive: palette.purple,     // timeline / history
  accentEditorial: palette.burgundy, // featured / premium
  accentSpecial: palette.gold,       // anniversary badges
  danger: '#D98A8A',

  crest: 'crest-white.png',
  crestWatermarkOpacity: 0.06,
} as const;

/** Light theme — parity build for system light mode. */
export const light = {
  surfacePage: palette.white,
  surfaceRaised: palette.warmWhite,
  surfaceCard: palette.white,
  surfaceSheet: palette.white,
  surfaceScrim: 'rgba(17,17,17,0.62)',
  surfaceBarBlur: 'rgba(255,255,255,0.90)',

  textPrimary: palette.charcoal,
  textSecondary: palette.grey600,
  textMuted: palette.grey500,
  textOnAccent: palette.white,

  hairline: palette.grey200,
  border: palette.grey200,
  borderStrong: palette.grey300,

  accentPrimary: palette.blue,       // on light, Royal Blue is the link/CTA colour
  accentPress: palette.blueDeep,
  accentTintWeak: '#ECF0F7',
  accentArchive: palette.purple,
  accentEditorial: palette.burgundy,
  accentSpecial: palette.gold,
  danger: '#8E2F2F',

  crest: 'crest-black.png',
  crestWatermarkOpacity: 0.05,
} as const;

/** Font families — bundle these as assets; do not fall back to system serif. */
export const fonts = {
  display: 'CormorantGaramond-Medium',      // 400/500/600 available
  displaySemi: 'CormorantGaramond-SemiBold',
  body: 'Inter-Regular',
  bodyMedium: 'Inter-Medium',
  bodySemi: 'Inter-SemiBold',
  titling: 'Cinzel-Regular',                // eyebrows / roman numerals ONLY, never body
} as const;

/**
 * Type scale — mobile values (points/dp). Cormorant carries hierarchy,
 * Inter carries reading and UI, Cinzel carries uppercase kickers.
 */
export const type = {
  heroTitle:   { fontFamily: fonts.display, fontSize: 38, lineHeight: 40, letterSpacing: -0.6 },
  pageTitle:   { fontFamily: fonts.display, fontSize: 34, lineHeight: 36, letterSpacing: -0.5 },
  articleTitle:{ fontFamily: fonts.display, fontSize: 28, lineHeight: 32, letterSpacing: -0.35 },
  cardTitle:   { fontFamily: fonts.display, fontSize: 21, lineHeight: 25, letterSpacing: -0.2 },
  pullQuote:   { fontFamily: fonts.display, fontSize: 26, lineHeight: 33 },
  dropCap:     { fontFamily: fonts.display, fontSize: 62, lineHeight: 51 },

  standfirst:  { fontFamily: fonts.body,   fontSize: 18, lineHeight: 29 },
  longform:    { fontFamily: fonts.body,   fontSize: 18, lineHeight: 31 }, // article body
  body:        { fontFamily: fonts.body,   fontSize: 16, lineHeight: 26 },
  listTitle:   { fontFamily: fonts.bodyMedium, fontSize: 15.5, lineHeight: 21 },
  caption:     { fontFamily: fonts.body,   fontSize: 13, lineHeight: 20 },
  meta:        { fontFamily: fonts.bodyMedium, fontSize: 10.5, lineHeight: 15, letterSpacing: 0.85, textTransform: 'uppercase' as const },

  eyebrow:     { fontFamily: fonts.titling, fontSize: 10, lineHeight: 14, letterSpacing: 2.2, textTransform: 'uppercase' as const },
  numeral:     { fontFamily: fonts.titling, fontSize: 26, lineHeight: 30, letterSpacing: 1.6 }, // MCMLXXV
} as const;

/** 4pt base scale. */
export const space = { xs: 4, sm: 8, md: 12, base: 16, lg: 20, xl: 24, xxl: 34, section: 44 } as const;

/** Restrained radii — pills only for chips/FAB. */
export const radius = { xs: 2, sm: 3, md: 4, pill: 999, fab: 18, sheet: 20, sheetIos: 14 } as const;

export const shadow = {
  card:  { shadowColor: '#000', shadowOpacity: 0.35, shadowRadius: 12, shadowOffset: { width: 0, height: 4 }, elevation: 3 },
  lift:  { shadowColor: '#000', shadowOpacity: 0.45, shadowRadius: 18, shadowOffset: { width: 0, height: 8 }, elevation: 6 },
  fab:   { shadowColor: '#000', shadowOpacity: 0.50, shadowRadius: 20, shadowOffset: { width: 0, height: 8 }, elevation: 8 },
} as const;

/** Motion — slow, elegant, never bouncy. */
export const motion = {
  fast: 180,
  base: 320,
  slow: 620,
  easing: [0.22, 0.61, 0.36, 1] as const, // use with Easing.bezier(...)
} as const;

/** Platform chrome metrics — the ONLY intended divergence between iOS and Android. */
export const chrome = {
  ios: {
    statusBarHeight: 47,
    navBarHeight: 44,
    navTitle: { fontFamily: fonts.bodySemi, fontSize: 17, letterSpacing: -0.2 },
    navTitleAlign: 'center' as const,
    backAffordance: 'chevron+label' as const,
    tabBarHeight: 83,       // includes 34pt home-indicator inset
    tabIcon: 25,
    tabLabel: 10,
    tabActiveStyle: 'tint' as const,
    sheet: 'actionSheet' as const, // floating card + separate Cancel button
    searchFieldRadius: radius.md + 6,
    pressFeedback: 'opacity' as const,
  },
  android: {
    statusBarHeight: 32,
    navBarHeight: 56,
    navTitle: { fontFamily: fonts.bodyMedium, fontSize: 20, letterSpacing: 0.1 },
    navTitleAlign: 'left' as const,
    backAffordance: 'arrow' as const,
    tabBarHeight: 80,
    tabIcon: 24,
    tabLabel: 11.5,
    tabActiveStyle: 'pill' as const, // Material 3 tinted pill behind icon
    sheet: 'bottomSheet' as const,   // edge-to-edge, drag handle, no Cancel
    searchFieldRadius: radius.pill,
    pressFeedback: 'ripple' as const,
    fabSize: 58,
  },
} as const;

/** Imagery rule: all archival photography renders monochrome. */
export const imagery = {
  grayscale: true,        // apply a saturation-0 colour matrix
  contrast: 1.05,
  scrimBottom: ['rgba(17,17,17,0.55)', 'rgba(17,17,17,0)', 'rgba(17,17,17,0.82)', '#111111'],
  scrimStops: [0, 0.32, 0.74, 1],
} as const;

export const theme = { palette, dark, light, fonts, type, space, radius, shadow, motion, chrome, imagery };
export type Theme = typeof theme;
export default theme;
