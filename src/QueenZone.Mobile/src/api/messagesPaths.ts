/** Member inbox paths for `/api/v1/me/messages` (issue #737). */

export const messagesApiPath = '/me/messages';

export const messagesUnreadCountPath = `${messagesApiPath}/unread-count`;

export function messagesConversationPath(conversationId: string): string {
  return `${messagesApiPath}/${conversationId}`;
}
