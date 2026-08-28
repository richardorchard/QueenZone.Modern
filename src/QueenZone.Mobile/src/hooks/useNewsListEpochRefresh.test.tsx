import { act, renderHook } from '@testing-library/react-native';
import { bumpNewsListEpoch } from '../notifications/newsListEpoch';
import { useNewsListEpochRefresh } from './useNewsListEpochRefresh';

describe('useNewsListEpochRefresh', () => {
  it('calls refresh when the news epoch bumps while mounted', () => {
    const refresh = jest.fn();
    renderHook(() => useNewsListEpochRefresh(refresh));

    act(() => {
      bumpNewsListEpoch();
    });

    expect(refresh).toHaveBeenCalledTimes(1);
  });

  it('does not call refresh after unmount', () => {
    const refresh = jest.fn();
    const { unmount } = renderHook(() => useNewsListEpochRefresh(refresh));
    unmount();

    act(() => {
      bumpNewsListEpoch();
    });

    expect(refresh).not.toHaveBeenCalled();
  });

  it('does not call refresh on the generation that was current at mount', () => {
    const refresh = jest.fn();
    renderHook(() => useNewsListEpochRefresh(refresh));
    expect(refresh).not.toHaveBeenCalled();
  });
});
