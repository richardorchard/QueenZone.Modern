import type { ApiPagedResponse, NewsDetail, NewsListItem } from '../api/types';
import type { MemberProfile } from '../api/me';
import type { AuthTokens } from '../api/auth';

export const apiOrigin = 'http://qz.test';

export function memberProfileFixture(overrides: Partial<MemberProfile> = {}): MemberProfile {
  return {
    memberId: 'member-1',
    email: 'freddie@qz.test',
    displayName: 'Freddie',
    createdAt: '1970-09-05T00:00:00.000Z',
    lastLoginAt: null,
    hasAvatar: false,
    avatarPath: null,
    avatarThumbPath: null,
    messagePrivacy: 'members',
    linkedProviders: ['Google'],
    legacyLink: {
      kind: 'none',
      match: null,
      claimableMatches: [],
      unavailableMatches: [],
    },
    scheduledDeletionAt: null,
    limits: {
      minDisplayNameLength: 2,
      maxDisplayNameLength: 100,
      maxAvatarBytes: 2_000_000,
      allowedAvatarContentTypes: ['image/jpeg'],
      deletionRetentionDays: 30,
    },
    deletion: {
      confirmationPhrase: 'DELETE',
      confirmationHint: 'Type DELETE',
      requestedTitle: 'Deletion requested',
      requestedMessage: 'Your account will be removed.',
      whatHappens: [],
    },
    ...overrides,
  };
}

export function memberProfilePayload(overrides: Record<string, unknown> = {}) {
  return {
    memberId: 'member-1',
    email: 'freddie@qz.test',
    displayName: 'Freddie',
    ...overrides,
  };
}

export function authTokensFixture(overrides: Partial<AuthTokens> = {}): AuthTokens {
  return {
    accessToken: 'access-token',
    refreshToken: 'refresh-token',
    expiresIn: 900,
    ...overrides,
  };
}

export function newsItemFixture(overrides: Partial<NewsListItem> = {}): NewsListItem {
  return {
    id: 42,
    title: 'Queen headline',
    excerpt: 'A restored archive story.',
    publishedAt: '2024-01-15T12:00:00.000Z',
    detailPath: '/news/42',
    ...overrides,
  };
}

export function newsDetailFixture(overrides: Partial<NewsDetail> = {}): NewsDetail {
  return {
    id: 42,
    title: 'Queen headline',
    excerpt: 'A restored archive story.',
    body: '<p>The restored article body.</p>',
    publishedAt: '2024-01-15T12:00:00.000Z',
    sourceUrl: null,
    detailPath: '/news/42',
    ...overrides,
  };
}

export function pagedResponse<T>(items: T[], page = 1, totalPages = 1): ApiPagedResponse<T> {
  return {
    items,
    page,
    pageSize: 20,
    totalCount: items.length,
    totalPages,
  };
}

export function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (reason?: unknown) => void;
  const promise = new Promise<T>((res, rej) => {
    resolve = res;
    reject = rej;
  });
  return { promise, resolve, reject };
}

export function jsonResponse(body: unknown, status = 200, headers: Record<string, string> = {}): Response {
  return new Response(status === 204 ? null : JSON.stringify(body), {
    status,
    headers: {
      'Content-Type': 'application/json',
      ...headers,
    },
  });
}
