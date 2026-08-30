import { getContentCache } from '../cache/defaultCache';
import type { ContentCache } from '../cache/contentCache';
import { conversationCacheKey } from '../cache/keys';
import { withOfflineCacheResult, type CachedResult } from '../cache/withOfflineCache';
import { fetchJson, sendJson } from './client';
import type { ApiPagedResponse } from './types';
import type { PageQuery } from './content';
import {
  messagesApiPath,
  messagesArchivedPath,
  messagesArchivePath,
  messagesBlockPath,
  messagesConversationPath,
  messagesRecipientsPath,
  messagesReportPath,
  messagesUnarchivePath,
  messagesUnblockPath,
  messagesUnreadCountPath,
} from './messagesPaths';

export {
  messagesApiPath,
  messagesArchivedPath,
  messagesArchivePath,
  messagesBlockPath,
  messagesConversationPath,
  messagesRecipientsPath,
  messagesReportPath,
  messagesUnarchivePath,
  messagesUnblockPath,
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
  reportedByViewer: boolean;
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
  hasBlockedOtherParticipant: boolean;
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

export function fetchArchivedInbox(
  accessToken: string,
  query: PageQuery = {},
): Promise<ApiPagedResponse<InboxConversation>> {
  return fetchJson(messagesArchivedPath, {
    query: pageParams(query),
    signal: query.signal,
    accessToken,
  });
}

export function archiveConversation(
  accessToken: string,
  conversationId: string,
  signal?: AbortSignal,
): Promise<void> {
  return sendJson(messagesArchivePath(conversationId), {
    method: 'POST',
    accessToken,
    signal,
  });
}

export function unarchiveConversation(
  accessToken: string,
  conversationId: string,
  signal?: AbortSignal,
): Promise<void> {
  return sendJson(messagesUnarchivePath(conversationId), {
    method: 'POST',
    accessToken,
    signal,
  });
}

/** Blocks the other participant in this conversation (matches website Conversation "Block" handler). */
export function blockConversationParticipant(
  accessToken: string,
  conversationId: string,
  signal?: AbortSignal,
): Promise<void> {
  return sendJson(messagesBlockPath(conversationId), {
    method: 'POST',
    accessToken,
    signal,
  });
}

export function unblockConversationParticipant(
  accessToken: string,
  conversationId: string,
  signal?: AbortSignal,
): Promise<void> {
  return sendJson(messagesUnblockPath(conversationId), {
    method: 'POST',
    accessToken,
    signal,
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

export type ConversationReadQuery = PageQuery & {
  cache?: ContentCache;
  /** Pull-to-refresh: write-through on success, never serve a cached snapshot. */
  networkOnly?: boolean;
  /** Signed-in profile.memberId. Required to cache; never the Bearer token. */
  memberId?: string | null;
};

export async function fetchConversationResult(
  accessToken: string,
  conversationId: string,
  query: ConversationReadQuery = {},
): Promise<CachedResult<ConversationDetail>> {
  const memberId = query.memberId?.trim() ?? '';
  const fetchFresh = () =>
    fetchJson<ConversationDetail>(messagesConversationPath(conversationId), {
      query: pageParams(query),
      signal: query.signal,
      accessToken,
    });

  if (!memberId) {
    const data = await fetchFresh();
    return { data, source: 'network', cachedAt: new Date().toISOString() };
  }

  const cache = query.cache ?? getContentCache();
  return withOfflineCacheResult(cache, conversationCacheKey(memberId, conversationId), fetchFresh, {
    fallback: query.networkOnly !== true,
    invalidateOn: [401, 403, 404],
  });
}

export async function fetchConversation(
  accessToken: string,
  conversationId: string,
  query: ConversationReadQuery = {},
): Promise<ConversationDetail> {
  return (await fetchConversationResult(accessToken, conversationId, query)).data;
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

export type MessageReportResult = {
  reportId: string;
  alreadyReported: boolean;
};

export function reportConversationMessage(
  accessToken: string,
  conversationId: string,
  messageId: string,
  reason?: string,
  signal?: AbortSignal,
): Promise<MessageReportResult> {
  return sendJson(messagesReportPath(conversationId, messageId), {
    method: 'POST',
    body: { reason: reason?.trim() ? reason.trim() : null },
    accessToken,
    signal,
  });
}
