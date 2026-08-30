/** Spec §4.3b Archive hub destinations, plus Trivia (#1101). No media requires. */
export const ARCHIVE_HUB_IDS = [
  'stories',
  'timeline',
  'biography',
  'discography',
  'tribute',
  'fan-performances',
  'recently-restored',
  'trivia',
  'about',
] as const;

export type ArchiveDestinationId = (typeof ARCHIVE_HUB_IDS)[number];
