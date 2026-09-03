export type AttachmentMetaInput = {
  extension: string;
  formattedSize: string;
};

export type AttachmentPreviewInput = {
  isImage: boolean;
  thumbnailUrl: string | null;
  url: string;
  extension?: string;
  fileName?: string;
};

export type AttachmentAction = 'none' | 'view-image' | 'play-audio' | 'open-file';

const AUDIO_EXTENSIONS = new Set([
  'mp3',
  'wav',
  'm4a',
  'aac',
  'ogg',
  'oga',
  'flac',
  'wma',
  'opus',
  'aiff',
  'aif',
  'weba',
  'caf',
]);

export function isAudioAttachment(item: {
  isImage?: boolean;
  extension?: string | null;
  fileName?: string | null;
  contentType?: string | null;
}): boolean {
  if (item.isImage) {
    return false;
  }
  const type = item.contentType?.trim().toLowerCase() ?? '';
  if (type.startsWith('audio/')) {
    return true;
  }
  const fromExtension = item.extension?.trim().replace(/^\./, '').toLowerCase() ?? '';
  if (fromExtension && AUDIO_EXTENSIONS.has(fromExtension)) {
    return true;
  }
  const name = item.fileName?.trim() ?? '';
  const dot = name.lastIndexOf('.');
  const fromName = dot >= 0 ? name.slice(dot + 1).toLowerCase() : '';
  return fromName.length > 0 && AUDIO_EXTENSIONS.has(fromName);
}

/** Matches website topic pages and `/api/v1/forum/topics/{id}/posts` (`ForumRoutes.PostsPageSize`). */
export const forumPostsPageSize = 15;

/** Category lists pass numeric ids; home prototype cards may still use strings. */
/** Reply is hidden when the topic header reports a lock (write API returns 403). */
export function topicReplyAllowed(topic: { isLocked: boolean } | null | undefined): boolean {
  return topic?.isLocked !== true;
}

export function watchButtonLabel(watching: boolean): string {
  return watching ? 'Unwatch' : 'Watch topic';
}

export function watchHint(watching: boolean): string {
  return watching
    ? "You're watching this topic. You'll get a push when someone else replies."
    : 'Watch to get a push when someone else replies. Posting does not subscribe you.';
}

export function parseTopicId(id: number | string): number | null {
  const value = typeof id === 'number' ? id : Number.parseInt(id, 10);
  if (!Number.isFinite(value) || value <= 0 || !Number.isInteger(value)) {
    return null;
  }
  return value;
}

export function formatPostTimestamp(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return '';
  }
  return date.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function formatMemberSince(iso: string | null | undefined): string | null {
  if (!iso) {
    return null;
  }
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return null;
  }
  return date.toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'long',
  });
}

export function attachmentMeta(item: AttachmentMetaInput): string {
  const parts = [item.extension, item.formattedSize, 'Members only'].filter(
    (part) => part.trim().length > 0,
  );
  return parts.join(' · ');
}

/**
 * Inline only a stored thumbnail that is not cookie-gated.
 * Empty or `/forum/attachment/...` → no Image. Do not fall back to
 * `url` / `downloadUrl` — those paths are member-gated and React Native
 * Image would hit them without auth.
 */
export function imagePreviewUrl(item: AttachmentPreviewInput): string | null {
  if (!item.isImage) {
    return null;
  }
  const thumb = item.thumbnailUrl?.trim() ?? '';
  if (thumb.length === 0) {
    return null;
  }
  // Same rule as isCookieGatedForumAttachmentPath — keep this file import-free
  // so the node:test suite can load it without React Native.
  if (
    /\/forum\/attachment(?:\/|$)/i.test(thumb) &&
    !/\/api\/v1\/forum\/attachments(?:\/|$)/i.test(thumb)
  ) {
    return null;
  }
  return thumb;
}

/**
 * Signed-in image always opens via Bearer `downloadUrl` (thumb or not).
 * Signed-in audio plays from a cached file URI. Other non-images share to Files.
 * Signed-out stays metadata-only.
 */
export function attachmentAction(
  item: AttachmentPreviewInput,
  signedIn: boolean,
): AttachmentAction {
  if (!signedIn) {
    return 'none';
  }
  if (item.isImage) {
    return 'view-image';
  }
  if (isAudioAttachment(item)) {
    return 'play-audio';
  }
  return 'open-file';
}
