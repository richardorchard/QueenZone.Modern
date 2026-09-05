import { Directory, File, Paths } from 'expo-file-system';
import { DOWNLOAD_DIRECTORY_NAME } from './types';

export type DownloadProbe = {
  sourceRevision: string | null;
  byteSize: number | null;
};

export type DownloadFileHost = {
  documentDirectoryUri(): string;
  availableBytes(): number;
  completedUri(performanceId: string): string;
  partUri(performanceId: string): string;
  exists(uri: string): boolean;
  size(uri: string): number;
  deleteIfExists(uri: string): void;
  listPartUris(): string[];
  listAllUris(): string[];
  promote(partUri: string, completedUri: string): void;
  writeBytes(uri: string, bytes: Uint8Array): void;
  download(input: {
    url: string;
    destUri: string;
    headers: Record<string, string>;
    onProgress?: (written: number, total: number) => void;
    signal?: AbortSignal;
  }): Promise<void>;
};

function joinUri(root: string, name: string): string {
  return `${root.replace(/\/+$/, '')}/${name}`;
}

export function opaqueFileName(performanceId: string, part = false): string {
  const id = performanceId.replace(/[^A-Za-z0-9_-]/g, '');
  return part ? `${id}.part` : id;
}

function createNativeHost(): DownloadFileHost {
  function audioDir(): Directory {
    const dir = new Directory(Paths.document, DOWNLOAD_DIRECTORY_NAME);
    if (!dir.exists) {
      dir.create({ intermediates: true, idempotent: true });
    }
    return dir;
  }

  function fileFor(uri: string): File {
    return new File(uri);
  }

  return {
    documentDirectoryUri() {
      return joinUri(Paths.document.uri, DOWNLOAD_DIRECTORY_NAME);
    },
    availableBytes() {
      const space = Paths.availableDiskSpace;
      return typeof space === 'number' && Number.isFinite(space) ? space : Number.POSITIVE_INFINITY;
    },
    completedUri(performanceId) {
      return new File(audioDir(), opaqueFileName(performanceId)).uri;
    },
    partUri(performanceId) {
      return new File(audioDir(), opaqueFileName(performanceId, true)).uri;
    },
    exists(uri) {
      try {
        return fileFor(uri).exists;
      } catch {
        return false;
      }
    },
    size(uri) {
      try {
        const file = fileFor(uri);
        return file.exists ? file.size : 0;
      } catch {
        return 0;
      }
    },
    deleteIfExists(uri) {
      try {
        const file = fileFor(uri);
        if (file.exists) {
          file.delete();
        }
      } catch {
        // Best-effort cleanup.
      }
    },
    listAllUris() {
      try {
        return audioDir()
          .list()
          .filter((entry): entry is File => entry instanceof File)
          .map((entry) => entry.uri);
      } catch {
        return [];
      }
    },
    listPartUris() {
      try {
        return audioDir()
          .list()
          .filter((entry): entry is File => entry instanceof File && entry.uri.endsWith('.part'))
          .map((entry) => entry.uri);
      } catch {
        return [];
      }
    },
    promote(partUri, completedUri) {
      const part = fileFor(partUri);
      const completed = fileFor(completedUri);
      if (completed.exists) {
        completed.delete();
      }
      part.move(completed);
    },
    writeBytes(uri, bytes) {
      const file = fileFor(uri);
      if (!file.exists) {
        file.create({ intermediates: true, overwrite: true });
      }
      file.write(bytes);
    },
    async download({ url, destUri, headers, onProgress, signal }) {
      audioDir();
      const dest = fileFor(destUri);
      if (dest.exists) {
        dest.delete();
      }
      const task = File.createDownloadTask(url, dest, {
        headers,
        signal,
        onProgress: onProgress
          ? ({ bytesWritten, totalBytes }) => {
              onProgress(bytesWritten, totalBytes);
            }
          : undefined,
      });
      const file = await task.downloadAsync();
      if (!file) {
        throw new Error('Download did not complete.');
      }
    },
  };
}

let host: DownloadFileHost = createNativeHost();

export function getDownloadFileHost(): DownloadFileHost {
  return host;
}

export function setDownloadFileHostForTests(next: DownloadFileHost | null): void {
  host = next ?? createNativeHost();
}

export function createMemoryDownloadHost(
  options: {
    availableBytes?: number;
    downloadImpl?: DownloadFileHost['download'];
  } = {},
): DownloadFileHost & { files: Map<string, Uint8Array> } {
  const files = new Map<string, Uint8Array>();
  const root = 'file:///documents/fan-performances';
  const downloadImpl =
    options.downloadImpl ??
    (async ({ destUri }) => {
      files.set(destUri, new Uint8Array([1, 2, 3, 4]));
    });

  return {
    files,
    documentDirectoryUri: () => root,
    availableBytes: () => options.availableBytes ?? 64 * 1024 * 1024,
    completedUri: (performanceId) => joinUri(root, opaqueFileName(performanceId)),
    partUri: (performanceId) => joinUri(root, opaqueFileName(performanceId, true)),
    exists: (uri) => files.has(uri),
    size: (uri) => files.get(uri)?.byteLength ?? 0,
    deleteIfExists: (uri) => {
      files.delete(uri);
    },
    listPartUris: () => [...files.keys()].filter((uri) => uri.endsWith('.part')),
    listAllUris: () => [...files.keys()],
    promote: (partUri, completedUri) => {
      const bytes = files.get(partUri);
      files.delete(partUri);
      if (bytes) {
        files.set(completedUri, bytes);
      }
    },
    writeBytes: (uri, bytes) => {
      files.set(uri, bytes);
    },
    download: downloadImpl,
  };
}
