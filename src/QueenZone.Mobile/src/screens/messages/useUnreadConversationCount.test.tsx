import { act, renderHook, waitFor } from '@testing-library/react-native';
import { fetchUnreadConversationCount } from '../../api/messages';
import { bumpNewsListEpoch } from '../../notifications/newsListEpoch';
import { bumpPmUnreadEpoch } from '../../notifications/pmUnreadEpoch';
import { createMockSession } from '../../test/mockSession';
import { useUnreadConversationCount } from './useUnreadConversationCount';

jest.mock('../../api/messages', () => ({
  fetchUnreadConversationCount: jest.fn(),
}));

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  // eslint-disable-next-line @typescript-eslint/no-require-imports -- Jest CJS mock factory.
  const { useEffect } = require('react');
  return {
    ...actual,
    useFocusEffect: (effect: () => void | (() => void)) => {
      useEffect(effect, [effect]);
    },
  };
});

const fetchUnread = fetchUnreadConversationCount as jest.MockedFunction<typeof fetchUnreadConversationCount>;

describe('useUnreadConversationCount', () => {
  beforeEach(() => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchUnread.mockReset();
    fetchUnread.mockResolvedValue(2);
  });

  it('fetches on focus and returns the API count', async () => {
    const { result } = renderHook(() => useUnreadConversationCount());
    await waitFor(() => expect(result.current).toBe(2));
    expect(fetchUnread).toHaveBeenCalledTimes(1);
  });

  it('fails soft to 0 when signed out without requesting', async () => {
    mockSession.isSignedIn = false;
    mockSession.accessToken = null;
    const { result } = renderHook(() => useUnreadConversationCount());
    await waitFor(() => expect(result.current).toBe(0));
    expect(fetchUnread).not.toHaveBeenCalled();
  });

  it('fails soft to 0 when the unread-count request fails', async () => {
    fetchUnread.mockRejectedValueOnce(new Error('offline'));
    const { result } = renderHook(() => useUnreadConversationCount());
    await waitFor(() => expect(result.current).toBe(0));
    expect(fetchUnread).toHaveBeenCalledTimes(1);
  });

  it('refetches when a PM epoch bump arrives while mounted', async () => {
    const { result } = renderHook(() => useUnreadConversationCount());
    await waitFor(() => expect(result.current).toBe(2));
    fetchUnread.mockResolvedValueOnce(5);

    await act(async () => {
      bumpPmUnreadEpoch();
    });

    await waitFor(() => expect(result.current).toBe(5));
    expect(fetchUnread).toHaveBeenCalledTimes(2);
  });

  it('skips the network after unmount when a PM epoch bump arrives', async () => {
    const { result, unmount } = renderHook(() => useUnreadConversationCount());
    await waitFor(() => expect(result.current).toBe(2));
    const calls = fetchUnread.mock.calls.length;
    unmount();

    await act(async () => {
      bumpPmUnreadEpoch();
    });

    expect(fetchUnread).toHaveBeenCalledTimes(calls);
  });

  it('keeps the last count when a superseded fetch aborts', async () => {
    const { result } = renderHook(() => useUnreadConversationCount());
    await waitFor(() => expect(result.current).toBe(2));
    const abortErr = new Error('aborted');
    abortErr.name = 'AbortError';
    fetchUnread.mockRejectedValueOnce(abortErr);

    await act(async () => {
      bumpPmUnreadEpoch();
    });

    expect(result.current).toBe(2);
  });

  it('does not refetch when the news list epoch bumps', async () => {
    const { result } = renderHook(() => useUnreadConversationCount());
    await waitFor(() => expect(result.current).toBe(2));
    const calls = fetchUnread.mock.calls.length;

    await act(async () => {
      bumpNewsListEpoch();
    });

    expect(fetchUnread).toHaveBeenCalledTimes(calls);
    expect(result.current).toBe(2);
  });
});
