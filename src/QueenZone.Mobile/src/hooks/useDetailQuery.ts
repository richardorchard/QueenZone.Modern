import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiError } from '../api/client';
import { PagedRequestCoordinator } from './usePagedContent';

function isAbortError(err: unknown): boolean {
  return err instanceof Error && err.name === 'AbortError';
}

function errorMessage(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Something went wrong.';
}

export type DetailQuery<T> = {
  data: T | null;
  error: string | null;
  loading: boolean;
  reload: () => void;
};

/**
 * Single-resource load with the same generation guard as {@link useHomeSection}.
 * An aborted first fetch on iOS can reject later as a TypeError / offline
 * ApiError; only the current generation may commit.
 */
export function useDetailQuery<T>(fetcher: (signal: AbortSignal) => Promise<T>): DetailQuery<T> {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const coordinatorRef = useRef<PagedRequestCoordinator | null>(null);
  if (coordinatorRef.current === null) {
    coordinatorRef.current = new PagedRequestCoordinator();
  }
  const coordinator = coordinatorRef.current;

  const run = useCallback(() => {
    const { generation, signal } = coordinator.begin();
    setLoading(true);
    setError(null);

    fetcher(signal)
      .then((result) => {
        if (!coordinator.isCurrent(generation)) {
          return;
        }
        setData(result);
        setError(null);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (!coordinator.isCurrent(generation) || signal.aborted || isAbortError(err)) {
          return;
        }
        setData(null);
        setError(errorMessage(err));
        setLoading(false);
      });
  }, [coordinator, fetcher]);

  useEffect(() => {
    run();
    return () => coordinator.invalidate();
  }, [coordinator, run]);

  return { data, error, loading, reload: run };
}
