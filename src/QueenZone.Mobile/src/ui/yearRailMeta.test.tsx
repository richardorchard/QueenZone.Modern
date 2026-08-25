import { buildYearTicks, offsetForYear, stepYear, yearForOffset } from './yearRailMeta';

describe('buildYearTicks', () => {
  it('returns descending years from max to min', () => {
    expect(buildYearTicks(2006, 2009)).toEqual([2009, 2008, 2007, 2006]);
  });

  it('returns a single-element array for a one-year archive', () => {
    expect(buildYearTicks(2026, 2026)).toEqual([2026]);
  });

  it('returns an empty array when the range is missing or inverted', () => {
    expect(buildYearTicks(null, 2026)).toEqual([]);
    expect(buildYearTicks(2026, null)).toEqual([]);
    expect(buildYearTicks(2026, 2006)).toEqual([]);
  });
});

describe('yearForOffset', () => {
  const years = [2026, 2025, 2024, 2023]; // newest first

  it('maps the top of the rail to the newest year', () => {
    expect(yearForOffset(0, 300, years)).toBe(2026);
  });

  it('maps the bottom of the rail to the oldest year', () => {
    expect(yearForOffset(300, 300, years)).toBe(2023);
  });

  it('maps the middle of the rail to a middle year', () => {
    expect(yearForOffset(150, 300, years)).toBe(2024);
  });

  it('clamps touches above or below the rail bounds', () => {
    expect(yearForOffset(-50, 300, years)).toBe(2026);
    expect(yearForOffset(9999, 300, years)).toBe(2023);
  });

  it('returns the only year when there is exactly one tick', () => {
    expect(yearForOffset(150, 300, [2026])).toBe(2026);
  });

  it('returns null when there are no years', () => {
    expect(yearForOffset(150, 300, [])).toBeNull();
  });
});

describe('offsetForYear', () => {
  const years = [2026, 2025, 2024, 2023];

  it('positions the newest year at the top', () => {
    expect(offsetForYear(2026, 300, years)).toBe(0);
  });

  it('positions the oldest year at the bottom', () => {
    expect(offsetForYear(2023, 300, years)).toBe(300);
  });

  it('returns 0 for a year not in the list', () => {
    expect(offsetForYear(1999, 300, years)).toBe(0);
  });
});

describe('stepYear', () => {
  const years = [2026, 2025, 2024, 2023];

  it('moves toward the oldest year with a positive delta', () => {
    expect(stepYear(2026, 1, years)).toBe(2025);
  });

  it('moves toward the newest year with a negative delta', () => {
    expect(stepYear(2025, -1, years)).toBe(2026);
  });

  it('clamps at the oldest year', () => {
    expect(stepYear(2023, 1, years)).toBe(2023);
  });

  it('clamps at the newest year', () => {
    expect(stepYear(2026, -1, years)).toBe(2026);
  });

  it('returns the current year unchanged when it is not in the list', () => {
    expect(stepYear(1999, 1, years)).toBe(1999);
  });
});
