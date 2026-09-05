/** Short file-size label for download UI. Null/invalid sizes stay hidden. */
export function formatByteSize(bytes: number | null | undefined): string {
  if (bytes == null || !Number.isFinite(bytes) || bytes < 0) {
    return '';
  }

  if (bytes < 1024) {
    return `${Math.round(bytes)} B`;
  }

  const kib = bytes / 1024;
  if (kib < 1024) {
    return `${kib < 10 ? kib.toFixed(1) : Math.round(kib)} KB`;
  }

  const mib = kib / 1024;
  return `${mib < 10 ? mib.toFixed(1) : Math.round(mib)} MB`;
}
