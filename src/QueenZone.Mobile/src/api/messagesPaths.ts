/** Member inbox paths for `/api/v1/me/messages` (issues #737 / #738 / #739). */

export const messagesApiPath = '/me/messages';

export const messagesUnreadCountPath = `${messagesApiPath}/unread-count`;

export const messagesRecipientsPath = `${messagesApiPath}/recipients`;

export function messagesConversationPath(conversationId: string): string {
  return `${messagesApiPath}/${conversationId}`;
}
