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
import { NEWS_LIST_CACHE_KEY } from '../../cache/keys';
import { useStoreRefresh } from '../../cache/useExternalStore';
import { useHomeSection } from '../../hooks/useHomeSection';
import { usePullToRefresh } from '../../hooks/usePullToRefresh';
import { syncHomeWidget } from '../../widgets/widgetSync';
import { onThisDayIsVisible, queenQuotesIsVisible } from './homeMeta';

/**
 * Owns every `useHomeSection` call for the home screen. This is the single place
 * they may live — pull-to-refresh must keep refetching a section's data even
 * while a filter chip hides that section's presentational component, so the
 * fetch hooks can never move into the section components themselves.
 */
export function useHomeScreenData(isSignedIn: boolean, accessToken: string | null) {
  const news = useHomeSection(useCallback((signal) => fetchNewsPage({ page: 1, pageSize: 4, signal }), []));
  useStoreRefresh(NEWS_LIST_CACHE_KEY, news.refresh);
  const forum = useHomeSection(useCallback((signal) => fetchForumRecentThreads(3, signal), []));
  const gallery = useHomeSection(
    useCallback((signal) => fetchPhotoCategories({ page: 1, pageSize: 3, signal }), []),
  );
  const onThisDay = useHomeSection(useCallback((signal) => fetchOnThisDay(signal), []));
  const quote = useHomeSection(useCallback((signal) => fetchRandomQuote(signal), []));
  const poll = useHomeSection(useCallback((signal) => fetchHomePoll(signal, accessToken), [accessToken]));
  const liveActivity = useHomeSection(useCallback((signal) => fetchLiveActivity(signal), []));
  const messages = useHomeSection(
    useCallback(
      (signal) =>
        isSignedIn && accessToken ? fetchInbox(accessToken, { pageSize: 2, signal }) : Promise.resolve(null),
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
