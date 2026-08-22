export type AttachmentMetaInput = {
  extension: string;
  formattedSize: string;
};

export type AttachmentPreviewInput = {
  isImage: boolean;
  thumbnailUrl: string | null;
  url: string;
};

/** Matches website topic pages and `/api/v1/forum/topics/{id}/posts` (`ForumRoutes.PostsPageSize`). */
export const forumPostsPageSize = 15;

/** Category lists pass numeric ids; home prototype cards may still use strings. */
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
 * Inline only when the website would: `isImage` plus a stored thumbnail.
 * Do not fall back to `/forum/attachment/...` — that path is member-gated and
 * React Native Image would hit it without auth.
 */
export function imagePreviewUrl(item: AttachmentPreviewInput): string | null {
  if (!item.isImage) {
    return null;
  }
  const thumb = item.thumbnailUrl?.trim() ?? '';
  return thumb.length > 0 ? thumb : null;
}
