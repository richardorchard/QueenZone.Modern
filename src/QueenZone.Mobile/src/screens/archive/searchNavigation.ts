import type { SearchResult } from '../../api/types';

function websiteUrl(apiBaseUrl: string, path: string): string | null {
  if (!path) {
    return null;
  }
  if (path.startsWith('http://') || path.startsWith('https://')) {
    return path;
  }
  const origin = apiBaseUrl.replace(/\/+$/, '');
  return `${origin}${path.startsWith('/') ? path : `/${path}`}`;
}

export type SearchTabTarget =
  | { kind: 'tab'; tab: 'NewsTab'; screen: 'Story'; params: { id: number } }
  | { kind: 'tab'; tab: 'ForumTab'; screen: 'Thread'; params: { id: number } }
  | { kind: 'tab'; tab: 'ArchiveTab'; screen: 'BiographyChapter'; params: { id: number } }
  | { kind: 'tab'; tab: 'ArchiveTab'; screen: 'Album'; params: { id: number } }
  | { kind: 'tab'; tab: 'ArchiveTab'; screen: 'Timeline' }
  | { kind: 'tab'; tab: 'ArchiveTab'; screen: 'FanPerformanceDetail'; params: { id: number } };

export type SearchOpenTarget = SearchTabTarget | { kind: 'web'; url: string } | { kind: 'unsupported' };

function positiveId(value: number | null | undefined): number | null {
  if (value == null || !Number.isInteger(value) || value <= 0) {
    return null;
  }
  return value;
}

/** Maps a live search hit to a native reader, or the website URL for article types. */
export function targetForSearchResult(item: SearchResult, apiBaseUrl: string): SearchOpenTarget {
  const contentType = item.contentType.trim().toLowerCase();

  if (contentType === 'news') {
    const id = positiveId(item.id);
    return id ? { kind: 'tab', tab: 'NewsTab', screen: 'Story', params: { id } } : { kind: 'unsupported' };
  }

  if (contentType === 'forum') {
    const id = positiveId(item.id);
    return id
      ? { kind: 'tab', tab: 'ForumTab', screen: 'Thread', params: { id } }
      : { kind: 'unsupported' };
  }

  if (contentType === 'biography') {
    const id = positiveId(item.id);
    return id
      ? { kind: 'tab', tab: 'ArchiveTab', screen: 'BiographyChapter', params: { id } }
      : { kind: 'unsupported' };
  }

  if (contentType === 'discography') {
    const id = positiveId(item.id);
    return id ? { kind: 'tab', tab: 'ArchiveTab', screen: 'Album', params: { id } } : { kind: 'unsupported' };
  }

  if (contentType === 'timeline') {
    return { kind: 'tab', tab: 'ArchiveTab', screen: 'Timeline' };
  }

  if (contentType === 'fan-performance') {
    const id = positiveId(item.id);
    return id
      ? { kind: 'tab', tab: 'ArchiveTab', screen: 'FanPerformanceDetail', params: { id } }
      : { kind: 'unsupported' };
  }

  if (contentType === 'article' || contentType === 'legacy-article') {
    const url = websiteUrl(apiBaseUrl, item.url);
    return url ? { kind: 'web', url } : { kind: 'unsupported' };
  }

  return { kind: 'unsupported' };
}

type TabNavigate = (tab: SearchTabTarget['tab'], params: { screen: string; params?: object }) => void;

/** Applies a mapped search target: tab navigation or in-app browser. */
export function applySearchTarget(
  target: SearchOpenTarget,
  navigate: TabNavigate,
  openUrl: (url: string) => void,
): void {
  if (target.kind === 'unsupported') {
    return;
  }
  if (target.kind === 'web') {
    openUrl(target.url);
    return;
  }
  if (target.screen === 'Timeline') {
    navigate(target.tab, { screen: target.screen });
    return;
  }
  navigate(target.tab, { screen: target.screen, params: target.params });
}
