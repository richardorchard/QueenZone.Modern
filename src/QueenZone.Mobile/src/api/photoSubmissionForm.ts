import type { PhotoSubmissionCreated } from './types';

/** Authenticated `POST /api/v1/member/photo-submissions` (issues #746 / #744). */
export const photoSubmissionsPath = '/member/photo-submissions';

export type PhotoUploadFile = {
  uri: string;
  name: string;
  type: string;
};

export type PhotoSubmissionFields = {
  title: string;
  description?: string;
  suggestedCategory?: string;
  approximateYear?: number;
  approximateDate?: string;
};

export function photoSubmissionFieldEntries(
  input: PhotoSubmissionFields,
): [string, string][] {
  const fields: [string, string][] = [['title', input.title.trim()]];
  const description = input.description?.trim();
  if (description) {
    fields.push(['description', description]);
  }

  const suggestedCategory = input.suggestedCategory?.trim();
  if (suggestedCategory) {
    fields.push(['suggestedCategory', suggestedCategory]);
  }

  if (input.approximateYear != null) {
    fields.push(['approximateYear', String(input.approximateYear)]);
  }

  const approximateDate = input.approximateDate?.trim();
  if (approximateDate) {
    fields.push(['approximateDate', approximateDate]);
  }

  return fields;
}

export function parsePhotoSubmissionCreated(payload: unknown): PhotoSubmissionCreated {
  if (!payload || typeof payload !== 'object') {
    throw new Error('Photo submission response was empty.');
  }

  const raw = payload as Record<string, unknown>;
  if (typeof raw.id !== 'string' || raw.id.trim() === '') {
    throw new Error('Photo submission response was missing an id.');
  }

  if (typeof raw.status !== 'string' || raw.status.trim() === '') {
    throw new Error('Photo submission response was missing a status.');
  }

  if (typeof raw.title !== 'string') {
    throw new Error('Photo submission response was missing a title.');
  }

  if (typeof raw.submittedAt !== 'string' || raw.submittedAt.trim() === '') {
    throw new Error('Photo submission response was missing a submitted time.');
  }

  return {
    id: raw.id,
    status: raw.status,
    title: raw.title,
    submittedAt: raw.submittedAt,
  };
}
