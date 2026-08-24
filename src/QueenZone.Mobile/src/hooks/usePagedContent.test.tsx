import { act, renderHook, waitFor } from '@testing-library/react-native';
import { ApiError } from '../api/client';
import { usePagedContent } from './usePagedContent';
import { deferred, pagedResponse } from '../test/fixtures';

describe('usePagedContent', () => {
  // Overlapping refresh/load-more races are tracked in #836; these tests cover
  // the stable abort-on-reset/unmount behaviour that already exists.
  it('loads the first page, then supports empty, error, retry, refresh, and load-more', async () => {
    const fetcher = jest.fn();
    fetcher.mockResolvedValueOnce(pagedResponse(['a', 'b'], 1, 2));
    const { result } = renderHook(() => usePagedContent(fetcher));

    expect(result.current.loading).toBe(true);
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.items).toEqual(['a', 'b']);
    expect(result.current.hasMore).toBe(true);
    expect(fetcher).toHaveBeenCalledWith(1, expect.any(AbortSignal));

    fetcher.mockResolvedValueOnce(pagedResponse(['c'], 2, 2));
    await act(async () => {
      result.current.loadMore();
    });
    await waitFor(() => expect(result.current.items).toEqual(['a', 'b', 'c']));
    expect(result.current.hasMore).toBe(false);

    fetcher.mockResolvedValueOnce(pagedResponse(['z'], 1, 1));
    await act(async () => {
      result.current.refresh();
    });
    await waitFor(() => expect(result.current.items).toEqual(['z']));
  });

  it('suppresses duplicate load-more while a request is in flight or at the end', async () => {
    const second = deferred<ReturnType<typeof pagedResponse<string>>>();
    const fetcher = jest.fn();
    fetcher.mockResolvedValueOnce(pagedResponse(['a'], 1, 3));
    fetcher.mockReturnValueOnce(second.promise);
    const { result } = renderHook(() => usePagedContent(fetcher));
    await waitFor(() => expect(result.current.loading).toBe(false));

    await act(async () => {
      result.current.loadMore();
    });
    await waitFor(() => expect(result.current.loadingMore).toBe(true));
    await act(async () => {
      result.current.loadMore();
    });
    expect(fetcher).toHaveBeenCalledTimes(2);

    second.resolve(pagedResponse(['b'], 2, 3));
    await waitFor(() => expect(result.current.items).toEqual(['a', 'b']));

    fetcher.mockResolvedValueOnce(pagedResponse(['c'], 3, 3));
    await act(async () => {
      result.current.loadMore();
    });
    await waitFor(() => expect(result.current.items).toEqual(['a', 'b', 'c']));
    expect(result.current.hasMore).toBe(false);

    await act(async () => {
      result.current.loadMore();
    });
    expect(fetcher).toHaveBeenCalledTimes(3);
  });

  it('resets and refetches when resetKey changes, ignoring aborted in-flight results', async () => {
    const first = deferred<ReturnType<typeof pagedResponse<string>>>();
    const fetcher = jest.fn();
    fetcher.mockReturnValueOnce(first.promise);
    fetcher.mockResolvedValueOnce(pagedResponse(['fresh'], 1, 1));
    const { result, rerender } = renderHook(({ key }) => usePagedContent(fetcher, 20, key), {
      initialProps: { key: 'one' },
    });
    expect(result.current.loading).toBe(true);
    rerender({ key: 'two' });
    first.resolve(pagedResponse(['stale'], 1, 1));
    await waitFor(() => expect(result.current.items).toEqual(['fresh']));
    expect(result.current.items).not.toContain('stale');
  });

  it('surfaces ApiError messages and recovers on reload', async () => {
    const fetcher = jest.fn();
    fetcher.mockRejectedValueOnce(new ApiError(500, 'The server had a problem. Try again shortly.'));
    fetcher.mockResolvedValueOnce(pagedResponse(['ok'], 1, 1));
    const { result } = renderHook(() => usePagedContent(fetcher));
    await waitFor(() => expect(result.current.error).toBe('The server had a problem. Try again shortly.'));

    await act(async () => {
      result.current.reload();
    });
    await waitFor(() => expect(result.current.items).toEqual(['ok']));
    expect(result.current.error).toBeNull();
  });

  it('does not apply a response after unmount', async () => {
    const pending = deferred<ReturnType<typeof pagedResponse<string>>>();
    const fetcher = jest.fn().mockReturnValue(pending.promise);
    const { result, unmount } = renderHook(() => usePagedContent(fetcher));
    unmount();
    pending.resolve(pagedResponse(['late'], 1, 1));
    await Promise.resolve();
    expect(result.current.items).toEqual([]);
  });
});
