/** Member inbox paths for `/api/v1/me/messages` (issues #737 / #738 / #739). */

export const messagesApiPath = '/me/messages';

export const messagesUnreadCountPath = `${messagesApiPath}/unread-count`;

export const messagesRecipientsPath = `${messagesApiPath}/recipients`;

export const messagesArchivedPath = `${messagesApiPath}/archived`;

export function messagesConversationPath(conversationId: string): string {
  return `${messagesApiPath}/${conversationId}`;
}

export function messagesReportPath(conversationId: string, messageId: string): string {
  return `${messagesConversationPath(conversationId)}/messages/${messageId}/report`;
}

export function messagesArchivePath(conversationId: string): string {
  return `${messagesConversationPath(conversationId)}/archive`;
}

export function messagesUnarchivePath(conversationId: string): string {
  return `${messagesConversationPath(conversationId)}/unarchive`;
}

export function messagesBlockPath(conversationId: string): string {
  return `${messagesConversationPath(conversationId)}/block`;
}

export function messagesUnblockPath(conversationId: string): string {
  return `${messagesConversationPath(conversationId)}/unblock`;
}
