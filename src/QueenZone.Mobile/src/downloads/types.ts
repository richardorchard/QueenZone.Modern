export type DownloadUiStatus = 'queued' | 'downloading' | 'downloaded' | 'failed' | 'removing';

export type DownloadUiSnapshot = {
  status: DownloadUiStatus;
  performanceId: string;
  title: string;
  performedBy: string;
  byteSize: number | null;
  expectedBytes: number | null;
  error: string | null;
};

export type DownloadManifestEntry = {
  performanceId: string;
  localUri: string;
  title: string;
  performedBy: string;
  byteSize: number | null;
  sourceRevision: string | null;
  completedAt: string;
  memberId: string;
};

export type DownloadManifest = {
  schemaVersion: typeof DOWNLOAD_MANIFEST_SCHEMA_VERSION;
  memberId: string;
  entries: Record<string, DownloadManifestEntry>;
};

export const DOWNLOAD_MANIFEST_SCHEMA_VERSION = 1;

export const DISK_SAFETY_MARGIN_BYTES = 8 * 1024 * 1024;

export const DOWNLOAD_DIRECTORY_NAME = 'fan-performances';
