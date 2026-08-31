/**
 * News runs roughly 2006-present (unlike biography/discography's much longer timeline), so its
 * decade chips cover a different span. `decadeStart` is `null` for "ALL" and otherwise the first
 * year of a 10-year server-side filter window (see `fetchNewsPage`'s `decade` param, issue #838).
 */
export const newsDecades = [
  { label: 'ALL', decadeStart: null },
  { label: '2020s', decadeStart: 2020 },
  { label: '2010s', decadeStart: 2010 },
  { label: '2000s', decadeStart: 2000 },
] as const;
