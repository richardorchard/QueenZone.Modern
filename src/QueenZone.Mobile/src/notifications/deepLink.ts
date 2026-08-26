import type { NotificationDestination } from './payload';

export type NotificationTabTarget = {
  screen: 'ForumTab' | 'HomeTab' | 'NewsTab';
  params: { screen: string; params: object; initial: false };
};

function tabParams<Screen extends string, Params extends object>(
  screen: Screen,
  params: Params,
): { screen: Screen; params: Params; initial: false } {
  return { screen, params, initial: false };
}

/** Nested-tab payload that keeps the tab root under the destination (back + tabs stay usable). */
export function notificationNavigateParams(destination: NotificationDestination): NotificationTabTarget {
  switch (destination.category) {
    case 'forumReply':
      return {
        screen: 'ForumTab',
        params: tabParams(
          'Thread',
          destination.postId === undefined
            ? { id: destination.topicId }
            : { id: destination.topicId, postId: destination.postId },
        ),
      };
    case 'privateMessage':
      return {
        screen: 'HomeTab',
        params: tabParams('Conversation', { id: destination.conversationId }),
      };
    case 'news':
      return {
        screen: 'NewsTab',
        params: tabParams('Story', { id: destination.articleId }),
      };
    default: {
      const _exhaustive: never = destination;
      return _exhaustive;
    }
  }
}

export type NotificationNavigation = {
  navigate: (name: 'Tabs', params: NotificationTabTarget) => void;
};

export function openNotificationDestination(
  navigation: NotificationNavigation,
  destination: NotificationDestination,
): void {
  navigation.navigate('Tabs', notificationNavigateParams(destination));
}
