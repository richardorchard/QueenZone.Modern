/** Percent when total is known; otherwise written bytes. Spinner-only is not enough. */
export function formatDownloadProgress(
  written: number | null | undefined,
  total: number | null | undefined,
): string {
  if (total != null && Number.isFinite(total) && total > 0 && written != null && Number.isFinite(written) && written >= 0) {
    return `${Math.min(100, Math.round((written / total) * 100))}%`;
  }
  return formatByteSize(written);
}

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
