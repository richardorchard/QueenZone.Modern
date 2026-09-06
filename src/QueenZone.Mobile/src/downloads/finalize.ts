import {
  DOWNLOAD_EMPTY_PART_MESSAGE,
  DOWNLOAD_FAILED_MESSAGE,
  DOWNLOAD_INCOMPLETE_MESSAGE,
  DOWNLOAD_PART_MISSING_MESSAGE,
  DOWNLOAD_TOO_SMALL_MESSAGE,
} from './messages';

/**
 * Real fan-performance audio is multi-megabyte. Problem Details / HTML error
 * pages and fake-complete native-task totals land well below this. A Range
 * probe that reports a smaller Content-Length is allowed through.
 */
export const MIN_PLAUSIBLE_AUDIO_BYTES = 8 * 1024;

export function resolveDownloadPartUri(destUri: string, returnedUri?: string | null): string {
  const returned = returnedUri?.trim();
  return returned ? returned : destUri;
}

/**
 * Prefer the Range-probe size over a much smaller progress total. Android
 * retries were racing to 100% on a tiny error/short body (`totalBytes` of
 * a few hundred bytes) and then failing at sniff / promote.
 */
export function adoptProgressTotal(
  probeExpected: number | null,
  progressTotal: number,
): number | null {
  if (!(progressTotal > 0) || !Number.isFinite(progressTotal)) {
    return probeExpected;
  }
  if (
    probeExpected != null &&
    probeExpected > MIN_PLAUSIBLE_AUDIO_BYTES &&
    progressTotal < probeExpected * 0.5
  ) {
    return probeExpected;
  }
  if (
    progressTotal < MIN_PLAUSIBLE_AUDIO_BYTES &&
    (probeExpected == null || probeExpected > MIN_PLAUSIBLE_AUDIO_BYTES)
  ) {
    return probeExpected;
  }
  return progressTotal;
}

export function isTinyCompleteDownload(input: {
  size: number;
  probeExpected: number | null;
  progressTotal: number | null;
}): boolean {
  const { size, probeExpected, progressTotal } = input;
  if (!(size > 0)) {
    return false;
  }
  if (probeExpected != null && probeExpected > 0 && probeExpected <= MIN_PLAUSIBLE_AUDIO_BYTES) {
    return false;
  }
  return (
    progressTotal != null &&
    progressTotal > 0 &&
    progressTotal < MIN_PLAUSIBLE_AUDIO_BYTES &&
    size <= progressTotal
  );
}

export function canPromotePart(exists: boolean, size: number): boolean {
  return exists && size > 0;
}

export function classifyPromoteError(error: unknown): 'no-such-file' | 'unknown' {
  if (!(error instanceof Error)) {
    return 'unknown';
  }
  const text = `${error.name} ${error.message}`;
  if (/NoSuchFile|ENOENT|not found|does not exist/i.test(text)) {
    return 'no-such-file';
  }
  return 'unknown';
}

export function messageForFinalizeFailure(
  kind: 'missing-part' | 'empty-part' | 'tiny-complete' | 'incomplete' | 'no-such-file' | 'unknown',
): string {
  switch (kind) {
    case 'missing-part':
    case 'no-such-file':
      return DOWNLOAD_PART_MISSING_MESSAGE;
    case 'empty-part':
      return DOWNLOAD_EMPTY_PART_MESSAGE;
    case 'tiny-complete':
      return DOWNLOAD_TOO_SMALL_MESSAGE;
    case 'incomplete':
      return DOWNLOAD_INCOMPLETE_MESSAGE;
    default:
      return DOWNLOAD_FAILED_MESSAGE;
  }
}
