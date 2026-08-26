import type { TimelineEvent } from '../../api/types';

export type HomeFilterKey = 'all' | 'news' | 'forum' | 'photography' | 'timeline';

export const homeFilters: { key: HomeFilterKey; label: string }[] = [
  { key: 'all', label: 'All' },
  { key: 'news', label: 'News' },
  { key: 'forum', label: 'Forum' },
  { key: 'photography', label: 'Photography' },
  { key: 'timeline', label: 'Timeline' },
];

export type HomeSectionKey = 'hero' | 'news' | 'forum' | 'gallery' | 'onThisDay';

const filterSections: Record<HomeFilterKey, HomeSectionKey[]> = {
  all: ['hero', 'news', 'forum', 'gallery', 'onThisDay'],
  news: ['hero', 'news'],
  forum: ['forum'],
  photography: ['gallery'],
  timeline: ['onThisDay'],
};

/**
 * Pure mapping from the selected filter chip to the set of sections that should render.
 * "Your messages" is driven by sign-in state, not by chip filter — it isn't one of the
 * five chip categories in the design hand-off.
 */
export function visibleSectionsForFilter(filter: HomeFilterKey): Set<HomeSectionKey> {
  return new Set(filterSections[filter]);
}

/**
 * Deterministic cycling index for content with no per-item image, matching the website
 * home page's `FeaturedArticleImages` cycling (`Pages/Index.cshtml.cs`) — news rows carry
 * no image in the data model. Kept id-based rather than index-based so the same news item
 * always maps to the same stock image across paginated fetches.
 */
export function stockImageIndexForId(id: number, imageCount: number): number {
  return ((id % imageCount) + imageCount) % imageCount;
}

export function formatForumThreadMeta(thread: {
  categoryName: string;
  replyCount: number;
  lastActivityAt: string;
}): string[] {
  return [thread.categoryName, `${thread.replyCount} replies`, relativeTimeFromNow(thread.lastActivityAt)];
}

export function formatGalleryCardMeta(category: { imageCount: number }): string {
  return `${category.imageCount.toLocaleString()} images`;
}

export function onThisDayIsVisible(event: TimelineEvent | null): event is TimelineEvent {
  return event !== null;
}

export function liveStripIsVisible(newForumRepliesToday: number): boolean {
  return newForumRepliesToday > 0;
}

export function liveStripLabel(newForumRepliesToday: number): string {
  return `${newForumRepliesToday} new forum ${newForumRepliesToday === 1 ? 'reply' : 'replies'} today`;
}

/** Short relative time ("just now", "20 min ago", "3 hr ago", falling back to a date). */
export function relativeTimeFromNow(iso: string, now: Date = new Date()): string {
  const then = new Date(iso);
  if (Number.isNaN(then.getTime())) {
    return '';
  }

  const diffMs = now.getTime() - then.getTime();
  const diffMinutes = Math.floor(diffMs / 60000);

  if (diffMinutes < 1) {
    return 'just now';
  }
  if (diffMinutes < 60) {
    return `${diffMinutes} min ago`;
  }
  const diffHours = Math.floor(diffMinutes / 60);
  if (diffHours < 24) {
    return `${diffHours} hr ago`;
  }
  const diffDays = Math.floor(diffHours / 24);
  if (diffDays < 7) {
    return `${diffDays} day${diffDays === 1 ? '' : 's'} ago`;
  }
  return then.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
}
