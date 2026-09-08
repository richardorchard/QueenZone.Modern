/** Problem Details / HTML error pages saved as a "download". */
export function looksLikeHttpErrorPayload(bytes: Uint8Array): boolean {
  let index = 0;
  while (
    index < bytes.length &&
    (bytes[index] === 0x09 || bytes[index] === 0x0a || bytes[index] === 0x0d || bytes[index] === 0x20)
  ) {
    index += 1;
  }
  if (index >= bytes.length) {
    return false;
  }
  const first = bytes[index];
  return first === 0x7b || first === 0x3c;
}

export type DownloadAudioExtension = 'mp3' | 'flac';

/**
 * iOS AVURLAsset relies on a local file's extension. Prefer the downloaded
 * bytes over response metadata so a stale or generic Content-Type cannot give
 * the completed file the wrong extension.
 */
export function resolveDownloadAudioExtension(
  bytes: Uint8Array | null,
  contentType?: string | null,
): DownloadAudioExtension {
  if (
    bytes &&
    bytes.length >= 4 &&
    bytes[0] === 0x66 &&
    bytes[1] === 0x4c &&
    bytes[2] === 0x61 &&
    bytes[3] === 0x43
  ) {
    return 'flac';
  }
  if (
    bytes &&
    ((bytes.length >= 3 && bytes[0] === 0x49 && bytes[1] === 0x44 && bytes[2] === 0x33) ||
      (bytes.length >= 2 && bytes[0] === 0xff && (bytes[1] & 0xe0) === 0xe0))
  ) {
    return 'mp3';
  }

  const normalized = contentType?.split(';', 1)[0]?.trim().toLowerCase();
  return normalized === 'audio/flac' || normalized === 'audio/x-flac' ? 'flac' : 'mp3';
}

export async function fileLooksLikeHttpError(
  readPrefix: (uri: string, maxBytes: number) => Promise<Uint8Array | null>,
  uri: string,
  size: number,
): Promise<boolean> {
  if (size <= 0) {
    return true;
  }
  // Real fan-performance audio is larger than a Problem Details / HTML body.
  // Only sniff small files so play/download does not load multi-megabyte tracks.
  if (size > 8192) {
    return false;
  }
  const prefix = await readPrefix(uri, 64);
  return prefix ? looksLikeHttpErrorPayload(prefix) : false;
}
