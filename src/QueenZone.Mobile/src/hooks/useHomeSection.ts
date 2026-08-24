import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiError } from '../api/client';

type SectionState<T> = {
  data: T | null;
  loading: boolean;
  error: string | null;
  reload: () => void;
};

/**
 * Loads a single non-paged `/api/v1` value (not a list — see `usePagedContent` for those).
 * Each call owns its own `AbortController`, so one section's failure or retry never blocks
 * another section's render — used by the home screen to paint independently per section
 * instead of gating first paint on one big `Promise.all`.
 */
export function useHomeSection<T>(
  fetcher: (signal: AbortSignal) => Promise<T>,
  deps: unknown[] = [],
): SectionState<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadToken, setReloadToken] = useState(0);
  const fetcherRef = useRef(fetcher);
  fetcherRef.current = fetcher;

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);

    fetcherRef
      .current(controller.signal)
      .then((result) => {
        if (controller.signal.aborted) {
          return;
        }
        setData(result);
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
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [reloadToken, ...deps]);

  return {
    data,
    loading,
    error,
    reload: useCallback(() => setReloadToken((n) => n + 1), []),
  };
}
