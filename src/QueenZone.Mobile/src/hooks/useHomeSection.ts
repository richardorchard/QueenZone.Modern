import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ApiError } from '../api/client';
import { PagedRequestCoordinator } from './usePagedContent';

export type SectionSnapshot<T> =
  | { status: 'pending'; data: T | null }
  | { status: 'ready'; data: T }
  | { status: 'failed'; data: T | null; message: string };

export type SectionView<T> =
  | { kind: 'skeleton' }
  | { kind: 'error'; message: string }
  | { kind: 'content'; data: T };

export function sectionViewOf<T>(snapshot: SectionSnapshot<T>): SectionView<T> {
  switch (snapshot.status) {
    case 'pending':
      return snapshot.data !== null ? { kind: 'content', data: snapshot.data } : { kind: 'skeleton' };
    case 'ready':
      return { kind: 'content', data: snapshot.data };
    case 'failed':
      return snapshot.data !== null
        ? { kind: 'content', data: snapshot.data }
        : { kind: 'error', message: snapshot.message };
  }
}

function isAbortError(err: unknown): boolean {
  return err instanceof Error && err.name === 'AbortError';
}

function errorMessage(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Something went wrong.';
}

export type HomeSection<T> = {
  view: SectionView<T>;
  reload: () => void;
  /**
   * Always resolves: success, mapped failure, abort, or supersede.
   */
  refresh: () => Promise<void>;
};

export function useHomeSection<T>(
  fetcher: (signal: AbortSignal) => Promise<T>,
  deps: unknown[] = [],
): HomeSection<T> {
  const [snapshot, setSnapshot] = useState<SectionSnapshot<T>>({ status: 'pending', data: null });
  const fetcherRef = useRef(fetcher);
  fetcherRef.current = fetcher;
  const coordinatorRef = useRef<PagedRequestCoordinator | null>(null);
  if (coordinatorRef.current === null) {
    coordinatorRef.current = new PagedRequestCoordinator();
  }
  const coordinator = coordinatorRef.current;

  const runSection = useCallback(
    async (mode: 'reload' | 'refresh'): Promise<void> => {
      const { generation, signal } = coordinator.begin();
      if (mode === 'reload') {
        setSnapshot((current) => ({ status: 'pending', data: current.data }));
      }

      try {
        const result = await fetcherRef.current(signal);
        if (!coordinator.isCurrent(generation)) {
          return;
        }
        setSnapshot({ status: 'ready', data: result });
      } catch (err: unknown) {
        if (!coordinator.isCurrent(generation) || isAbortError(err)) {
          return;
        }
        setSnapshot((current) => ({
          status: 'failed',
          data: current.data,
          message: errorMessage(err),
        }));
      }
    },
    [coordinator],
  );

  useEffect(() => {
    void runSection('reload');
    return () => coordinator.invalidate();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [coordinator, ...deps]);

  const reload = useCallback(() => {
    void runSection('reload');
  }, [runSection]);

  const refresh = useCallback(() => runSection('refresh'), [runSection]);
  const view = useMemo(() => sectionViewOf(snapshot), [snapshot]);

  return { view, reload, refresh };
}
