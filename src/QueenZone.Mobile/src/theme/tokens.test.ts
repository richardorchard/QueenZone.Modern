/**
 * Locks RN theme hex values to the web design-system CSS tokens (#792).
 * Values are copied from colors.css / spacing.css — fail this test if they drift.
 */
import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { dark, fonts, light, motion, palette, radius, space } from './tokens.ts';

/** Expected values from `wwwroot/design-system/tokens/colors.css`. */
const webColors = {
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
  blue: '#244A8F',
  blueDeep: '#1B3A72',
  blueTint: '#ECF0F7',
  purple: '#5D3A8A',
  purpleDeep: '#492C6E',
  purpleTint: '#F0ECF5',
  burgundy: '#6B1F33',
  burgundyDeep: '#551828',
  burgundyTint: '#F6ECEE',
  gold: '#B89A4A',
  goldDeep: '#9C8038',
  goldTint: '#F6F1E3',
} as const;

describe('palette matches web design-system colors.css', () => {
  for (const [key, expected] of Object.entries(webColors)) {
    it(`palette.${key} === ${expected}`, () => {
      assert.equal(palette[key as keyof typeof webColors], expected);
    });
  }
});

describe('semantic roles', () => {
  it('uses gold as dark accentPrimary (contrast on #111)', () => {
    assert.equal(dark.accentPrimary, webColors.gold);
    assert.equal(dark.surfacePage, webColors.black);
    assert.equal(dark.accentArchive, webColors.purple);
    assert.equal(dark.accentEditorial, webColors.burgundy);
  });

  it('uses royal blue as light accentPrimary (web --link)', () => {
    assert.equal(light.accentPrimary, webColors.blue);
    assert.equal(light.surfacePage, webColors.white);
    assert.equal(light.textPrimary, webColors.charcoal);
    assert.equal(light.hairline, webColors.grey200);
  });
});

describe('spacing and motion match spacing.css foundation', () => {
  it('keeps the 4px base scale (--space-1…5 / gutter)', () => {
    assert.equal(space.xs, 4);
    assert.equal(space.sm, 8);
    assert.equal(space.md, 12);
    assert.equal(space.base, 16);
    assert.equal(space.xl, 24);
  });

  it('keeps restrained radii (--radius-xs/sm/md/pill)', () => {
    assert.equal(radius.xs, 2);
    assert.equal(radius.sm, 3);
    assert.equal(radius.md, 4);
    assert.equal(radius.pill, 999);
  });

  it('keeps motion durations and ease-out curve', () => {
    assert.equal(motion.fast, 180);
    assert.equal(motion.base, 320);
    assert.equal(motion.slow, 620);
    assert.deepEqual([...motion.easing], [0.22, 0.61, 0.36, 1]);
  });
});

describe('font family roles', () => {
  it('names the three web typefaces with distinct RN family keys', () => {
    assert.equal(fonts.display, 'CormorantGaramond-Medium');
    assert.equal(fonts.displaySemi, 'CormorantGaramond-SemiBold');
    assert.equal(fonts.body, 'Inter-Regular');
    assert.equal(fonts.bodyMedium, 'Inter-Medium');
    assert.equal(fonts.bodySemi, 'Inter-SemiBold');
    assert.equal(fonts.titling, 'Cinzel-Regular');
  });
});
