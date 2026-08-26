/**
 * Local crest and placeholder photography from the v2 mobile handoff.
 * Archival photography is already monochrome in these files.
 */
export const media = {
  crestWhite: require('../../assets/archive/crest-white.png') as number,
  crestBlack: require('../../assets/archive/crest-black.png') as number,
  crestSilver: require('../../assets/archive/crest-silver.png') as number,
  /** Tight crop, wordmark cropped off — used by the boot splash hero + watermark. */
  crestEmblem: require('../../assets/archive/crest-emblem.png') as number,
  hero: require('../../assets/archive/img-hero.jpg') as number,
  crowd: require('../../assets/archive/img-crowd.jpg') as number,
  portrait: require('../../assets/archive/img-portrait.jpg') as number,
  stage: require('../../assets/archive/img-stage.jpg') as number,
  studio: require('../../assets/archive/img-studio.jpg') as number,
} as const;

export type MediaKey = keyof typeof media;
