import { useCallback, useEffect } from 'react';
import {
  fetchForumRecentThreads,
  fetchHomePoll,
  fetchInbox,
  fetchLiveActivity,
  fetchNewsPage,
  fetchOnThisDay,
  fetchPhotoCategories,
  fetchRandomQuote,
} from '../../api';
import {
  HOME_FORUM_THREADS_CACHE_KEY,
  HOME_LIVE_ACTIVITY_CACHE_KEY,
  HOME_NEWS_CACHE_KEY,
  HOME_ON_THIS_DAY_CACHE_KEY,
  HOME_PHOTO_CATEGORIES_CACHE_KEY,
  HOME_QUOTE_CACHE_KEY,
  NEWS_LIST_CACHE_KEY,
} from '../../cache/keys';
import { HOME_LIVE_TTL_MS, HOME_MODERATE_TTL_MS, HOME_SLOW_CHANGING_TTL_MS } from '../../cache/ttl';
import { useStoreRefresh } from '../../cache/useExternalStore';
import { useHomeSection } from '../../hooks/useHomeSection';
import { usePullToRefresh } from '../../hooks/usePullToRefresh';
import { syncHomeWidget } from '../../widgets/widgetSync';
import { onThisDayIsVisible, queenQuotesIsVisible } from './homeMeta';
import { inboxPageSize } from '../messages/inboxMeta';

/**
 * Reload may be served a fresh-enough cached value (TTL, issue #1477);
 * pull-to-refresh must always hit the network, same as every other
 * pull-to-refresh in the app.
 */
function ttlUnlessRefresh(mode: 'reload' | 'refresh', ttlMs: number) {
  return mode === 'refresh' ? { fallback: false as const } : { ttlMs };
}

/**
 * Owns every `useHomeSection` call for the home screen. This is the single place
 * they may live — pull-to-refresh must keep refetching a section's data even
 * while a filter chip hides that section's presentational component, so the
 * fetch hooks can never move into the section components themselves.
 */
export function useHomeScreenData(isSignedIn: boolean, accessToken: string | null) {
  const news = useHomeSection(
    useCallback(
      (signal, mode) =>
        fetchNewsPage({
          page: 1,
          pageSize: 4,
          signal,
          cacheHint: { cacheKey: HOME_NEWS_CACHE_KEY, ...ttlUnlessRefresh(mode, HOME_MODERATE_TTL_MS) },
        }),
      [],
    ),
  );
  useStoreRefresh(NEWS_LIST_CACHE_KEY, news.refresh);
  const forum = useHomeSection(
    useCallback(
      (signal, mode) =>
        fetchForumRecentThreads(3, signal, {
          cacheKey: HOME_FORUM_THREADS_CACHE_KEY,
          ...ttlUnlessRefresh(mode, HOME_MODERATE_TTL_MS),
        }),
      [],
    ),
  );
  const gallery = useHomeSection(
    useCallback(
      (signal, mode) =>
        fetchPhotoCategories({
          page: 1,
          pageSize: 3,
          signal,
          cacheHint: {
            cacheKey: HOME_PHOTO_CATEGORIES_CACHE_KEY,
            ...ttlUnlessRefresh(mode, HOME_MODERATE_TTL_MS),
          },
        }),
      [],
    ),
  );
  const onThisDay = useHomeSection(
    useCallback(
      (signal, mode) =>
        fetchOnThisDay(signal, {
          cacheKey: HOME_ON_THIS_DAY_CACHE_KEY,
          ...ttlUnlessRefresh(mode, HOME_SLOW_CHANGING_TTL_MS),
        }),
      [],
    ),
  );
  const quote = useHomeSection(
    useCallback(
      (signal, mode) =>
        fetchRandomQuote(signal, {
          cacheKey: HOME_QUOTE_CACHE_KEY,
          ...ttlUnlessRefresh(mode, HOME_SLOW_CHANGING_TTL_MS),
        }),
      [],
    ),
  );
  const poll = useHomeSection(useCallback((signal) => fetchHomePoll(signal, accessToken), [accessToken]));
  const liveActivity = useHomeSection(
    useCallback(
      (signal, mode) =>
        fetchLiveActivity(signal, {
          cacheKey: HOME_LIVE_ACTIVITY_CACHE_KEY,
          ...ttlUnlessRefresh(mode, HOME_LIVE_TTL_MS),
        }),
      [],
    ),
  );
  const messages = useHomeSection(
    useCallback(
      (signal, mode) =>
        isSignedIn && accessToken
          ? fetchInbox(accessToken, {
              page: 1,
              pageSize: inboxPageSize,
              signal,
              networkOnly: mode === 'refresh',
              ttlMs: HOME_LIVE_TTL_MS,
            }).then((page) => ({
              ...page,
              items: page.items.slice(0, 2),
            }))
          : Promise.resolve(null),
      [isSignedIn, accessToken],
    ),
  );

  const pull = usePullToRefresh([
    news.refresh,
    forum.refresh,
    gallery.refresh,
    onThisDay.refresh,
    quote.refresh,
    poll.refresh,
    liveActivity.refresh,
    messages.refresh,
  ]);

  useEffect(() => {
    if (onThisDay.view.kind === 'skeleton' || quote.view.kind === 'skeleton') {
      return;
    }
    syncHomeWidget({
      onThisDay: onThisDay.view.kind === 'content' ? onThisDay.view.data : null,
      quote: quote.view.kind === 'content' ? quote.view.data : null,
    }).catch(() => {
      /* widget sync is best-effort */
    });
  }, [onThisDay.view, quote.view]);

  const newsItems = news.view.kind === 'content' ? news.view.data.items : [];
  const hero = newsItems[0] ?? null;
  const latestNews = newsItems.slice(1, 4);
  const totalNewsCount = news.view.kind === 'content' ? news.view.data.totalCount : 0;
  const onThisDayEvent = onThisDay.view.kind === 'content' ? onThisDay.view.data : null;
  const featuredQuote = quote.view.kind === 'content' ? quote.view.data : null;
  const homePoll = poll.view.kind === 'content' ? poll.view.data : null;
  const onThisDayQuote = featuredQuote ? { text: featuredQuote.text, whoSaid: featuredQuote.whoSaid } : null;

  return {
    news,
    forum,
    gallery,
    onThisDay,
    quote,
    poll,
    liveActivity,
    messages,
    pull,
    newsItems,
    hero,
    latestNews,
    totalNewsCount,
    onThisDayEvent,
    featuredQuote,
    homePoll,
    onThisDayQuote,
    onThisDayEventVisible: onThisDayIsVisible(onThisDayEvent),
    queenQuotesVisible: queenQuotesIsVisible(onThisDayQuote),
  };
}

export type HomeScreenData = ReturnType<typeof useHomeScreenData>;
