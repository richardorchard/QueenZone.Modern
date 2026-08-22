/** Spec §4.3b — the eight Archive hub destinations. No media requires. */
export const ARCHIVE_HUB_IDS = [
  'stories',
  'timeline',
  'biography',
  'discography',
  'tribute',
  'fan-performances',
  'recently-restored',
  'about',
] as const;

export type ArchiveDestinationId = (typeof ARCHIVE_HUB_IDS)[number];
