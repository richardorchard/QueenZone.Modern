import * as Notifications from 'expo-notifications';
import {
  isDefaultNotificationTap,
  noticeFromNotification,
  subscribeNotificationEvents,
  tapFromResponse,
} from './subscribe';

const addNotificationResponseReceivedListener =
  Notifications.addNotificationResponseReceivedListener as jest.MockedFunction<
    typeof Notifications.addNotificationResponseReceivedListener
  >;
const addNotificationReceivedListener = Notifications.addNotificationReceivedListener as jest.MockedFunction<
  typeof Notifications.addNotificationReceivedListener
>;
const getLastNotificationResponseAsync = Notifications.getLastNotificationResponseAsync as jest.MockedFunction<
  typeof Notifications.getLastNotificationResponseAsync
>;
const clearLastNotificationResponseAsync = Notifications.clearLastNotificationResponseAsync as jest.MockedFunction<
  typeof Notifications.clearLastNotificationResponseAsync
>;

const conversationId = '11111111-2222-3333-4444-555555555555';

function notification(data: Record<string, unknown>, identifier = 'req-1') {
  return {
    date: 1,
    request: {
      identifier,
      content: {
        title: 'Ranking every studio album',
        body: 'New reply',
        data,
      },
    },
  } as Notifications.Notification;
}

function response(data: Record<string, unknown>, identifier = 'req-1'): Notifications.NotificationResponse {
  return {
    actionIdentifier: Notifications.DEFAULT_ACTION_IDENTIFIER,
    notification: notification(data, identifier),
  };
}

describe('tapFromResponse', () => {
  it('maps a default tap to a #757 destination', () => {
    expect(tapFromResponse(response({ category: 'forumReply', topicId: '12', postId: '34' }))).toEqual({
      identifier: 'req-1',
      destination: { category: 'forumReply', topicId: 12, postId: 34 },
    });
  });

  it('ignores non-default actions and unmapped payloads', () => {
    expect(
      tapFromResponse({
        actionIdentifier: 'dismiss',
        notification: notification({ category: 'forumReply', topicId: '12' }),
      }),
    ).toBeNull();
    expect(tapFromResponse(response({ category: 'news' }))).toBeNull();
    expect(tapFromResponse(null)).toBeNull();
  });
});

describe('noticeFromNotification', () => {
  it('uses the payload title/body and falls back to the #757 copy', () => {
    expect(noticeFromNotification(notification({ category: 'privateMessage', conversationId }))).toEqual({
      identifier: 'req-1',
      title: 'Ranking every studio album',
      body: 'New reply',
      destination: { category: 'privateMessage', conversationId },
    });

    const bare = {
      date: 1,
      request: {
        identifier: 'req-2',
        content: { title: null, body: '   ', data: { category: 'news', articleId: '88' } },
      },
    } as Notifications.Notification;
    expect(noticeFromNotification(bare)).toEqual({
      identifier: 'req-2',
      title: 'New QueenZone article',
      body: 'New article published.',
      destination: { category: 'news', articleId: 88 },
    });
  });
});

describe('isDefaultNotificationTap', () => {
  it('accepts Expo\'s default action identifier', () => {
    expect(isDefaultNotificationTap(Notifications.DEFAULT_ACTION_IDENTIFIER)).toBe(true);
    expect(isDefaultNotificationTap('other')).toBe(false);
  });
});

describe('subscribeNotificationEvents', () => {
  let responseListener: ((value: Notifications.NotificationResponse) => void) | undefined;
  let receivedListener: ((value: Notifications.Notification) => void) | undefined;

  beforeEach(() => {
    responseListener = undefined;
    receivedListener = undefined;
    addNotificationResponseReceivedListener.mockImplementation((cb) => {
      responseListener = cb;
      return { remove: jest.fn() };
    });
    addNotificationReceivedListener.mockImplementation((cb) => {
      receivedListener = cb;
      return { remove: jest.fn() };
    });
    getLastNotificationResponseAsync.mockReset().mockResolvedValue(null);
    clearLastNotificationResponseAsync.mockReset().mockResolvedValue();
  });

  it('opens a cold-start last response once and clears it', async () => {
    getLastNotificationResponseAsync.mockResolvedValue(response({ category: 'news', articleId: '88' }, 'cold'));
    const onTap = jest.fn();
    const onForeground = jest.fn();

    const sub = subscribeNotificationEvents({ onTap, onForeground });
    await Promise.resolve();
    await Promise.resolve();

    expect(onTap).toHaveBeenCalledTimes(1);
    expect(onTap).toHaveBeenCalledWith({
      identifier: 'cold',
      destination: { category: 'news', articleId: 88 },
    });
    expect(clearLastNotificationResponseAsync).toHaveBeenCalled();

    responseListener?.(response({ category: 'news', articleId: '88' }, 'cold'));
    expect(onTap).toHaveBeenCalledTimes(1);

    sub.remove();
  });

  it('opens a background tap from the response listener', async () => {
    const onTap = jest.fn();
    subscribeNotificationEvents({ onTap, onForeground: jest.fn() });
    await Promise.resolve();

    responseListener?.(response({ category: 'forumReply', topicId: '1002' }, 'warm'));

    expect(onTap).toHaveBeenCalledWith({
      identifier: 'warm',
      destination: { category: 'forumReply', topicId: 1002 },
    });
  });

  it('surfaces a foreground receipt for the in-app banner', async () => {
    const onForeground = jest.fn();
    subscribeNotificationEvents({ onTap: jest.fn(), onForeground });
    await Promise.resolve();

    receivedListener?.(notification({ category: 'privateMessage', conversationId }, 'fg'));

    expect(onForeground).toHaveBeenCalledWith({
      identifier: 'fg',
      title: 'Ranking every studio album',
      body: 'New reply',
      destination: { category: 'privateMessage', conversationId },
    });
  });

  it('does not handle a last response after unsubscribe', async () => {
    let resolveLast: (value: Notifications.NotificationResponse | null) => void = () => {};
    getLastNotificationResponseAsync.mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveLast = resolve;
        }),
    );
    const onTap = jest.fn();
    const sub = subscribeNotificationEvents({ onTap, onForeground: jest.fn() });
    sub.remove();
    resolveLast(response({ category: 'news', articleId: '1' }, 'late'));
    await Promise.resolve();
    expect(onTap).not.toHaveBeenCalled();
  });
});
