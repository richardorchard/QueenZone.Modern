export type CategoryMetaInput = {
  latestThreadTitle: string | null;
  lastActivityAt: string | null;
  postCount: number;
};

export type TopicMetaInput = {
  lastActivityAt: string;
  replyCount: number;
  lastPostUsername: string | null;
  isSticky: boolean;
};

export function formatForumCount(value: number): string {
  return value.toLocaleString();
}

export function forumIndexStatItems(input: {
  boardCount: number;
  threadCount: number;
  postCount: number;
}): { value: string; label: string }[] {
  return [
    { value: formatForumCount(input.boardCount), label: 'Boards' },
    { value: formatForumCount(input.threadCount), label: 'Threads' },
    { value: formatForumCount(input.postCount), label: 'Posts' },
  ];
}

function formatListDate(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return '';
  }
  return date.toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

export function categoryMeta(item: CategoryMetaInput): string {
  if (item.latestThreadTitle) {
    return `Latest: ${item.latestThreadTitle}`;
  }
  if (item.lastActivityAt) {
    const date = formatListDate(item.lastActivityAt);
    return date ? `Last activity ${date}` : `${formatForumCount(item.postCount)} posts`;
  }
  return `${formatForumCount(item.postCount)} posts`;
}

export function topicMeta(item: TopicMetaInput): string {
  const parts: string[] = [];
  if (item.isSticky) {
    parts.push('Pinned');
  }
  parts.push(`${formatForumCount(item.replyCount)} replies`);
  if (item.lastPostUsername) {
    parts.push(item.lastPostUsername);
  }
  const date = formatListDate(item.lastActivityAt);
  if (date) {
    parts.push(date);
  }
  return parts.join(' · ');
}
