import { act, renderHook } from '@testing-library/react-native';
import { invalidate, invalidatePrefix, resetExternalStoreForTests } from './externalStore';
import { NEWS_CACHE_KEY_PREFIX, NEWS_LIST_CACHE_KEY, PM_UNREAD_CACHE_KEY } from './keys';
import { usePrefixVersion, useStoreRefresh, useStoreVersion } from './useExternalStore';

describe('useExternalStore', () => {
  beforeEach(() => {
    resetExternalStoreForTests();
  });

  it('re-renders useStoreVersion when the key is invalidated', () => {
    const { result } = renderHook(() => useStoreVersion(NEWS_LIST_CACHE_KEY));
    expect(result.current).toBe(0);

    act(() => {
      invalidate(NEWS_LIST_CACHE_KEY);
    });

    expect(result.current).toBe(1);
  });

  it('re-renders usePrefixVersion for matching key and prefix invalidation only', () => {
    const { result } = renderHook(() => usePrefixVersion(NEWS_CACHE_KEY_PREFIX));
    expect(result.current).toBe(0);

    act(() => {
      invalidate(PM_UNREAD_CACHE_KEY);
    });
    expect(result.current).toBe(0);

    act(() => {
      invalidate(NEWS_LIST_CACHE_KEY);
    });
    expect(result.current).toBe(1);

    act(() => {
      invalidatePrefix(NEWS_CACHE_KEY_PREFIX);
    });
    expect(result.current).toBe(2);
  });

  it('calls refresh when the key is invalidated while mounted', () => {
    const refresh = jest.fn();
    renderHook(() => useStoreRefresh(NEWS_LIST_CACHE_KEY, refresh));

    act(() => {
      invalidate(NEWS_LIST_CACHE_KEY);
    });

    expect(refresh).toHaveBeenCalledTimes(1);
  });

  it('does not call refresh after unmount', () => {
    const refresh = jest.fn();
    const { unmount } = renderHook(() => useStoreRefresh(NEWS_LIST_CACHE_KEY, refresh));
    unmount();

    act(() => {
      invalidate(NEWS_LIST_CACHE_KEY);
    });

    expect(refresh).not.toHaveBeenCalled();
  });

  it('does not call refresh on the generation that was current at mount', () => {
    const refresh = jest.fn();
    renderHook(() => useStoreRefresh(NEWS_LIST_CACHE_KEY, refresh));
    expect(refresh).not.toHaveBeenCalled();
  });
});
