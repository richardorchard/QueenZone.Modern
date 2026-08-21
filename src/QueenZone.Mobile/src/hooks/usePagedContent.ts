import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiError } from '../api/client';
import type { ApiPagedResponse } from '../api/types';

type Fetcher<T> = (page: number, signal: AbortSignal) => Promise<ApiPagedResponse<T>>;

type PagedState<T> = {
  items: T[];
  page: number;
  totalPages: number;
  totalCount: number;
  loading: boolean;
  refreshing: boolean;
  loadingMore: boolean;
  error: string | null;
  hasMore: boolean;
  reload: () => void;
  refresh: () => void;
  loadMore: () => void;
};

/**
 * Loads a paged `/api/v1` collection with pull-to-refresh and infinite scroll.
 */
export function usePagedContent<T>(fetcher: Fetcher<T>, pageSize = 20): PagedState<T> {
  const [items, setItems] = useState<T[]>([]);
  const [page, setPage] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [reloadToken, setReloadToken] = useState(0);
  const fetcherRef = useRef(fetcher);
  fetcherRef.current = fetcher;

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);
    setItems([]);
    setPage(0);
    setTotalPages(0);
    setTotalCount(0);

    fetcherRef
      .current(1, controller.signal)
      .then((response) => {
        if (controller.signal.aborted) {
          return;
        }
        setItems(response.items);
        setPage(response.page);
        setTotalPages(response.totalPages);
        setTotalCount(response.totalCount);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (controller.signal.aborted || (err instanceof Error && err.name === 'AbortError')) {
          return;
        }
        const message = err instanceof ApiError ? err.message : 'Something went wrong.';
        setError(message);
        setLoading(false);
      });

    return () => controller.abort();
  }, [reloadToken, pageSize]);

  const refresh = useCallback(() => {
    const controller = new AbortController();
    setRefreshing(true);
    setError(null);
    fetcherRef
      .current(1, controller.signal)
      .then((response) => {
        setItems(response.items);
        setPage(response.page);
        setTotalPages(response.totalPages);
        setTotalCount(response.totalCount);
        setRefreshing(false);
      })
      .catch((err: unknown) => {
        if (err instanceof Error && err.name === 'AbortError') {
          return;
        }
        const message = err instanceof ApiError ? err.message : 'Something went wrong.';
        setError(message);
        setRefreshing(false);
      });
  }, []);

  const loadMore = useCallback(() => {
    if (loading || refreshing || loadingMore || page >= totalPages || totalPages === 0) {
      return;
    }
    const nextPage = page + 1;
    const controller = new AbortController();
    setLoadingMore(true);
    fetcherRef
      .current(nextPage, controller.signal)
      .then((response) => {
        setItems((prev) => [...prev, ...response.items]);
        setPage(response.page);
        setTotalPages(response.totalPages);
        setTotalCount(response.totalCount);
        setLoadingMore(false);
      })
      .catch((err: unknown) => {
        if (err instanceof Error && err.name === 'AbortError') {
          return;
        }
        setLoadingMore(false);
      });
  }, [loading, refreshing, loadingMore, page, totalPages]);

  return {
    items,
    page,
    totalPages,
    totalCount,
    loading,
    refreshing,
    loadingMore,
    error,
    hasMore: totalPages > 0 && page < totalPages,
    reload: () => setReloadToken((n) => n + 1),
    refresh,
    loadMore,
  };
}
