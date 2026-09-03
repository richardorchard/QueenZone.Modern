import { EncodingType, cacheDirectory, writeAsStringAsync } from 'expo-file-system/legacy';

export function cacheFileName(fileName: string): string {
  const trimmed = fileName.trim();
  const base = trimmed.split(/[/\\]/).pop()?.trim() ?? '';
  return base.length > 0 ? base : 'attachment';
}

/** Write bytes to the app cache using the real file name. Returns a `file://` URI. */
export async function writeCachedLocalFile(fileName: string, bytes: Uint8Array): Promise<string> {
  const directory = cacheDirectory;
  if (!directory) {
    throw new Error('Unable to write a cache file.');
  }
  const dest = `${directory}${cacheFileName(fileName)}`;
  await writeAsStringAsync(dest, bytesToBase64(bytes), { encoding: EncodingType.Base64 });
  return dest;
}

function bytesToBase64(bytes: Uint8Array): string {
  let binary = '';
  for (let i = 0; i < bytes.length; i += 1) {
    binary += String.fromCharCode(bytes[i]!);
  }
  return globalThis.btoa(binary);
}
