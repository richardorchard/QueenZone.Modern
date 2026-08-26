import type { NewsSuggestionCreated } from './types';

/** Authenticated `POST /api/v1/member/news-suggestions` (issue #926). */
export const newsSuggestionsPath = '/member/news-suggestions';

export function parseNewsSuggestionCreated(payload: unknown): NewsSuggestionCreated {
  if (!payload || typeof payload !== 'object') {
    throw new Error('News suggestion response was empty.');
  }

  const raw = payload as Record<string, unknown>;
  if (typeof raw.id !== 'string' || raw.id.trim() === '') {
    throw new Error('News suggestion response was missing an id.');
  }

  if (typeof raw.status !== 'string' || raw.status.trim() === '') {
    throw new Error('News suggestion response was missing a status.');
  }

  if (typeof raw.url !== 'string' || raw.url.trim() === '') {
    throw new Error('News suggestion response was missing a url.');
  }

  if (raw.title != null && typeof raw.title !== 'string') {
    throw new Error('News suggestion response had an invalid title.');
  }

  if (typeof raw.submittedAt !== 'string' || raw.submittedAt.trim() === '') {
    throw new Error('News suggestion response was missing a submitted time.');
  }

  return {
    id: raw.id,
    status: raw.status,
    url: raw.url,
    title: raw.title == null ? null : raw.title,
    submittedAt: raw.submittedAt,
  };
}
