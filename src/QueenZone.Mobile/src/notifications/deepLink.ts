import { nestedTabParams } from '../navigation/nestedTab';
import type { NotificationDestination } from './payload';

export type NotificationTabTarget = {
  screen: 'ForumTab' | 'HomeTab' | 'NewsTab';
  params: { screen: string; params: object; initial: false };
};

/** Nested-tab payload that keeps the tab root under the destination (back + tabs stay usable). */
export function notificationNavigateParams(destination: NotificationDestination): NotificationTabTarget {
  switch (destination.category) {
    case 'forumReply':
      return {
        screen: 'ForumTab',
        params: nestedTabParams(
          'Thread',
          destination.postId === undefined
            ? { id: destination.topicId }
            : { id: destination.topicId, postId: destination.postId },
        ),
      };
    case 'privateMessage':
      return {
        screen: 'HomeTab',
        params: nestedTabParams('Conversation', { id: destination.conversationId }),
      };
    case 'news':
      return {
        screen: 'NewsTab',
        params: nestedTabParams('Story', { id: destination.articleId }),
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
