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
