import { fetchJson, sendJson } from './client';
import type { ApiPagedResponse } from './types';
import type { PageQuery } from './content';
import {
  messagesApiPath,
  messagesConversationPath,
  messagesRecipientsPath,
  messagesUnreadCountPath,
} from './messagesPaths';

export {
  messagesApiPath,
  messagesConversationPath,
  messagesRecipientsPath,
  messagesUnreadCountPath,
} from './messagesPaths';

export type InboxConversation = {
  conversationId: string;
  otherParticipantId: string;
  otherParticipantDisplayName: string;
  lastMessagePreview: string;
  lastMessageAt: string;
  hasUnread: boolean;
  unreadCount: number;
  detailPath: string;
};

export type ConversationMessage = {
  id: string;
  senderMemberId: string;
  senderDisplayName: string;
  body: string;
  createdAt: string;
  isMine: boolean;
  sortKey: number;
};

export type ConversationDetail = {
  conversationId: string;
  otherParticipantId: string;
  otherParticipantDisplayName: string;
  messages: ConversationMessage[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  detailPath: string;
  canSendReply: boolean;
};

export type MessageRecipient = {
  memberId: string;
  displayName: string;
};

function pageParams({ page, pageSize }: PageQuery) {
  return { page, pageSize };
}

export function fetchInbox(
  accessToken: string,
  query: PageQuery = {},
): Promise<ApiPagedResponse<InboxConversation>> {
  return fetchJson(messagesApiPath, {
    query: pageParams(query),
    signal: query.signal,
    accessToken,
  });
}

export function fetchUnreadConversationCount(accessToken: string, signal?: AbortSignal): Promise<number> {
  return fetchJson<{ unreadConversationCount: number }>(messagesUnreadCountPath, {
    accessToken,
    signal,
  }).then((payload) =>
    typeof payload.unreadConversationCount === 'number' && Number.isFinite(payload.unreadConversationCount)
      ? Math.max(0, Math.trunc(payload.unreadConversationCount))
      : 0,
  );
}

export function searchRecipients(
  accessToken: string,
  query: string,
  signal?: AbortSignal,
): Promise<MessageRecipient[]> {
  return fetchJson<{ items: MessageRecipient[] }>(messagesRecipientsPath, {
    query: { q: query },
    accessToken,
    signal,
  }).then((payload) => (Array.isArray(payload.items) ? payload.items : []));
}

export function fetchConversation(
  accessToken: string,
  conversationId: string,
  query: PageQuery = {},
): Promise<ConversationDetail> {
  return fetchJson(messagesConversationPath(conversationId), {
    query: pageParams(query),
    signal: query.signal,
    accessToken,
  });
}

export function replyToConversation(
  accessToken: string,
  conversationId: string,
  body: string,
  signal?: AbortSignal,
): Promise<ConversationDetail> {
  return sendJson(messagesConversationPath(conversationId), {
    method: 'POST',
    body: { body },
    accessToken,
    signal,
  });
}

export function composeMessage(
  accessToken: string,
  recipientMemberId: string,
  body: string,
  signal?: AbortSignal,
): Promise<ConversationDetail> {
  return sendJson(messagesApiPath, {
    method: 'POST',
    body: { recipientMemberId, body },
    accessToken,
    signal,
  });
}
