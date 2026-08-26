import { ApiError } from './errors';
import { shouldUseNativeMultipartUpload } from './nativeUpload';

export type UploadFilePart = {
  uri: string;
  name: string;
  type: string;
};

/**
 * React Native XMLHttpRequest understands this file object. Do not convert
 * it to a Blob first — `fetch(file://)` throws on the same iOS builds.
 */
export function appendNativeUploadFile(form: FormData, fieldName: string, file: UploadFilePart): void {
  form.append(fieldName, {
    uri: file.uri,
    name: file.name,
    type: file.type,
  } as unknown as Blob);
}

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
 * Read a picker `file://` / `content://` URI into a real Blob for Node
 * contract tests and any runtime that still posts multipart through fetch.
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
  if (shouldUseNativeMultipartUpload()) {
    appendNativeUploadFile(form, fieldName, file);
    return;
  }

  const blob = await readUploadFileBlob(file, signal);
  form.append(fieldName, blob, file.name);
}
