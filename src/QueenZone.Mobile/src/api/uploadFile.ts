import { ApiError } from './errors';

export type UploadFilePart = {
  uri: string;
  name: string;
  type: string;
};

function isAbortError(err: unknown): boolean {
  return err instanceof Error && err.name === 'AbortError';
}

function typedBlob(blob: Blob, type: string): Blob {
  if (!type || blob.type === type) {
    return blob;
  }

  return new Blob([blob], { type });
}

/**
 * Read a picker `file://` / `content://` URI into a real Blob so FormData
 * does not depend on React Native's `{ uri, name, type }` file part.
 * That object throws `TypeError: Network request failed` on some iOS
 * TestFlight builds even for a small crop.
 */
export async function readUploadFileBlob(file: UploadFilePart, signal?: AbortSignal): Promise<Blob> {
  let response: Response;
  try {
    response = await fetch(file.uri, { signal });
  } catch (err) {
    if (isAbortError(err)) {
      throw err;
    }
    throw ApiError.localFile(err);
  }

  if (!response.ok) {
    throw ApiError.localFile();
  }

  let blob: Blob;
  try {
    blob = await response.blob();
  } catch (err) {
    if (isAbortError(err)) {
      throw err;
    }
    throw ApiError.localFile(err);
  }

  if (blob.size <= 0) {
    throw ApiError.localFile();
  }

  return typedBlob(blob, file.type);
}

export async function appendUploadFile(
  form: FormData,
  fieldName: string,
  file: UploadFilePart,
  signal?: AbortSignal,
): Promise<void> {
  const blob = await readUploadFileBlob(file, signal);
  form.append(fieldName, blob, file.name);
}
