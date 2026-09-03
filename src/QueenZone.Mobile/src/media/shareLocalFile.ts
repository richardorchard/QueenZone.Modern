import * as Sharing from 'expo-sharing';

export function isShareCanceled(error: unknown): boolean {
  const message = error instanceof Error ? error.message : String(error ?? '');
  return /cancel|dismissed|ERR_SHARING_CANCELLED/i.test(message);
}

/**
 * System share / Save to Files for a cached `file://` URI.
 * User cancel is not an error. Never pass a data URI.
 */
export async function shareLocalFile(
  fileUri: string,
  contentType: string,
  fileName: string,
): Promise<void> {
  if (!fileUri.startsWith('file:')) {
    throw new Error('Unable to share this file.');
  }
  const available = await Sharing.isAvailableAsync();
  if (!available) {
    throw new Error('Unable to share this file.');
  }

  try {
    await Sharing.shareAsync(fileUri, {
      mimeType: contentType,
      dialogTitle: fileName,
    });
  } catch (error: unknown) {
    if (isShareCanceled(error)) {
      return;
    }
    throw error;
  }
}
