import * as Notifications from 'expo-notifications';
import { noteNewsListPush } from './newsListEpoch';
import { fallbackNoticeCopy, parseNotificationData, type NotificationDestination } from './payload';

export type NotificationTap = {
  identifier: string;
  destination: NotificationDestination;
};

export type ForegroundNotice = {
  identifier: string;
  title: string;
  body: string;
  destination: NotificationDestination;
};

export type NotificationEventHandlers = {
  onTap: (tap: NotificationTap) => void;
  onForeground: (notice: ForegroundNotice) => void;
};

function defaultActionIdentifier(): string {
  return Notifications.DEFAULT_ACTION_IDENTIFIER;
}

export function isDefaultNotificationTap(actionIdentifier: string): boolean {
  return actionIdentifier === defaultActionIdentifier();
}

/**
 * expo-notifications copies APNs `userInfo.body` into `content.data`.
 * Direct APNs puts the #757 keys beside `aps`, so `content.data` is often
 * empty on iOS and the keys live on `trigger.payload`.
 */
function pushTriggerPayload(trigger: Notifications.Notification['request']['trigger']): unknown {
  if (trigger == null || typeof trigger !== 'object' || !('type' in trigger) || trigger.type !== 'push') {
    return undefined;
  }

  return 'payload' in trigger ? trigger.payload : undefined;
}

function destinationFromNotification(notification: Notifications.Notification): NotificationDestination | null {
  return (
    parseNotificationData(notification.request.content.data) ??
    parseNotificationData(pushTriggerPayload(notification.request.trigger))
  );
}

export function tapFromResponse(response: Notifications.NotificationResponse | null | undefined): NotificationTap | null {
  if (!response || !isDefaultNotificationTap(response.actionIdentifier)) {
    return null;
  }

  const destination = destinationFromNotification(response.notification);
  if (!destination) {
    return null;
  }

  return {
    identifier: response.notification.request.identifier,
    destination,
  };
}

export function noticeFromNotification(notification: Notifications.Notification): ForegroundNotice | null {
  const destination = destinationFromNotification(notification);
  if (!destination) {
    return null;
  }

  const fallback = fallbackNoticeCopy(destination);
  const title = notification.request.content.title?.trim();
  const body = notification.request.content.body?.trim();

  return {
    identifier: notification.request.identifier,
    title: title && title.length > 0 ? title : fallback.title,
    body: body && body.length > 0 ? body : fallback.body,
    destination,
  };
}

/**
 * Cold start (`getLastNotificationResponseAsync`) and warm/background taps
 * (`addNotificationResponseReceivedListener`) share one handler. Duplicate
 * deliveries of the same request identifier are ignored.
 */
export function subscribeNotificationEvents(handlers: NotificationEventHandlers): { remove: () => void } {
  const handledTaps = new Set<string>();
  let cancelled = false;

  function handleResponse(response: Notifications.NotificationResponse | null | undefined): void {
    const tap = tapFromResponse(response);
    if (!tap || handledTaps.has(tap.identifier)) {
      return;
    }

    handledTaps.add(tap.identifier);
    void Notifications.clearLastNotificationResponseAsync();
    noteNewsListPush(tap.destination);
    handlers.onTap(tap);
  }

  function handleReceived(notification: Notifications.Notification): void {
    const notice = noticeFromNotification(notification);
    if (notice) {
      noteNewsListPush(notice.destination);
      handlers.onForeground(notice);
    }
  }

  const responseSub = Notifications.addNotificationResponseReceivedListener(handleResponse);
  const receivedSub = Notifications.addNotificationReceivedListener(handleReceived);

  void Notifications.getLastNotificationResponseAsync().then((response) => {
    if (!cancelled) {
      handleResponse(response);
    }
  });

  return {
    remove: () => {
      cancelled = true;
      responseSub.remove();
      receivedSub.remove();
    },
  };
}
