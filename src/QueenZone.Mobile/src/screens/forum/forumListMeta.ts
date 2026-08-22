import { formatPublishedDate } from '../../api';
import type { ForumCategoryListItem, ForumTopicListItem } from '../../api';

export function formatForumCount(value: number): string {
  return value.toLocaleString();
}

export function categoryMeta(item: ForumCategoryListItem): string {
  if (item.latestThreadTitle) {
    return `Latest: ${item.latestThreadTitle}`;
  }
  if (item.lastActivityAt) {
    const date = formatPublishedDate(item.lastActivityAt);
    return date ? `Last activity ${date}` : `${formatForumCount(item.postCount)} posts`;
  }
  return `${formatForumCount(item.postCount)} posts`;
}

export function topicMeta(item: ForumTopicListItem): string {
  const parts: string[] = [];
  if (item.isSticky) {
    parts.push('Pinned');
  }
  parts.push(`${formatForumCount(item.replyCount)} replies`);
  if (item.lastPostUsername) {
    parts.push(item.lastPostUsername);
  }
  const date = formatPublishedDate(item.lastActivityAt);
  if (date) {
    parts.push(date);
  }
  return parts.join(' · ');
}
