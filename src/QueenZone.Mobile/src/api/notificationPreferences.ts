/** Member notification category toggles for `/api/v1/me/notification-preferences` (#758 / #852). */

import { fetchJson, sendJson } from './client';

export const notificationPreferencesApiPath = '/me/notification-preferences';

export type NotificationPreferences = {
  forumReply: boolean;
  privateMessage: boolean;
  news: boolean;
};

export type NotificationPreferencePatch = {
  forumReply?: boolean;
  privateMessage?: boolean;
  news?: boolean;
};

export type NotificationPreferenceKey = keyof NotificationPreferences;

/** Server defaults from #758: forum replies on, messages on, news off. */
export const defaultNotificationPreferences: NotificationPreferences = {
  forumReply: true,
  privateMessage: true,
  news: false,
};

export function parseNotificationPreferences(payload: unknown): NotificationPreferences {
  if (!payload || typeof payload !== 'object' || Array.isArray(payload)) {
    throw new Error('Notification preferences response was empty.');
  }

  const raw = payload as Record<string, unknown>;
  if (
    typeof raw.forumReply !== 'boolean' ||
    typeof raw.privateMessage !== 'boolean' ||
    typeof raw.news !== 'boolean'
  ) {
    throw new Error('Notification preferences response was missing category toggles.');
  }

  return {
    forumReply: raw.forumReply,
    privateMessage: raw.privateMessage,
    news: raw.news,
  };
}

export async function fetchNotificationPreferences(accessToken: string): Promise<NotificationPreferences> {
  return parseNotificationPreferences(await fetchJson(notificationPreferencesApiPath, { accessToken }));
}

export async function patchNotificationPreferences(
  accessToken: string,
  patch: NotificationPreferencePatch,
): Promise<NotificationPreferences> {
  return parseNotificationPreferences(
    await sendJson(notificationPreferencesApiPath, {
      method: 'PATCH',
      accessToken,
      body: patch,
    }),
  );
}
