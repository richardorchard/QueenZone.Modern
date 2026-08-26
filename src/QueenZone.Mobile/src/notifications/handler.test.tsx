import * as Notifications from 'expo-notifications';
import { configureForegroundNotificationHandler, foregroundNotificationBehavior } from './handler';

const setNotificationHandler = Notifications.setNotificationHandler as jest.MockedFunction<
  typeof Notifications.setNotificationHandler
>;

describe('configureForegroundNotificationHandler', () => {
  it('shows the notification in the tray but not a second OS banner (in-app toast owns foreground)', async () => {
    configureForegroundNotificationHandler();

    expect(setNotificationHandler).toHaveBeenCalledTimes(1);
    const handler = setNotificationHandler.mock.calls[0]?.[0];
    expect(handler).toBeTruthy();
    const behavior = handler && 'handleNotification' in handler ? await handler.handleNotification({} as never) : null;
    expect(behavior).toEqual(foregroundNotificationBehavior);
    expect(foregroundNotificationBehavior.shouldShowBanner).toBe(false);
    expect(foregroundNotificationBehavior.shouldShowList).toBe(true);
  });
});
