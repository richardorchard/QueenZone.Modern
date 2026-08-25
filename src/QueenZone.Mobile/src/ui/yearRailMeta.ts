/**
 * Pure layout/selection math for {@link YearRail}, split out so it can be unit tested without
 * touching gesture plumbing or React Native. Years are always laid out newest-at-top, oldest-at-
 * bottom, matching the reading order of the decade chips it complements (issue #886).
 */

/** Descending years from `maxYear` to `minYear`, inclusive. Empty when the range is invalid. */
export function buildYearTicks(minYear: number | null, maxYear: number | null): number[] {
  if (minYear === null || maxYear === null || maxYear < minYear) {
    return [];
  }

  const ticks: number[] = [];
  for (let year = maxYear; year >= minYear; year -= 1) {
    ticks.push(year);
  }
  return ticks;
}

/**
 * Maps a touch offset within the rail to the nearest year tick. `offsetY` and `railHeight` are
 * in the same coordinate space (the rail's local layout). Clamps out-of-bounds touches to the
 * nearest end so a drag that overshoots the rail still resolves to the first/last year rather
 * than doing nothing.
 */
export function yearForOffset(offsetY: number, railHeight: number, years: readonly number[]): number | null {
  if (years.length === 0) {
    return null;
  }
  if (years.length === 1 || railHeight <= 0) {
    return years[0];
  }

  const ratio = Math.min(1, Math.max(0, offsetY / railHeight));
  const index = Math.round(ratio * (years.length - 1));
  return years[index];
}

/** Vertical center of a year's tick, for positioning the floating label bubble. */
export function offsetForYear(year: number, railHeight: number, years: readonly number[]): number {
  const index = years.indexOf(year);
  if (index === -1 || years.length <= 1) {
    return 0;
  }

  return (index / (years.length - 1)) * railHeight;
}

/**
 * Year at `indexDelta` positions away from `currentYear` in the (newest-first) `years` list,
 * clamped to the ends. Used for VoiceOver/TalkBack increment/decrement accessibility actions.
 */
export function stepYear(currentYear: number, indexDelta: number, years: readonly number[]): number {
  const index = years.indexOf(currentYear);
  if (index === -1) {
    return currentYear;
  }

  const nextIndex = Math.min(years.length - 1, Math.max(0, index + indexDelta));
  return years[nextIndex];
}
