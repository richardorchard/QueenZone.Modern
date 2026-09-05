export const fanPerformanceSubmissionsPath = '/member/fan-performance-submissions';

export type AudioUploadFile = {
  uri: string;
  name: string;
  type: string;
};

export type FanPerformanceSubmissionFields = {
  title: string;
  coveredSong: string;
  performedBy: string;
  description?: string;
  rightsDeclarationAccepted: boolean;
};

export type FanPerformanceSubmissionCreated = {
  id: string;
  status: string;
  title: string;
  submittedAt: string;
};

export function fanPerformanceSubmissionFieldEntries(
  input: FanPerformanceSubmissionFields,
): [string, string][] {
  const fields: [string, string][] = [
    ['title', input.title.trim()],
    ['coveredSong', input.coveredSong.trim()],
    ['performedBy', input.performedBy.trim()],
    ['rightsDeclarationAccepted', input.rightsDeclarationAccepted ? 'true' : 'false'],
  ];
  const description = input.description?.trim();
  if (description) {
    fields.push(['description', description]);
  }

  return fields;
}

export function parseFanPerformanceSubmissionCreated(payload: unknown): FanPerformanceSubmissionCreated {
  if (!payload || typeof payload !== 'object') {
    throw new Error('Fan performance submission response was empty.');
  }

  const raw = payload as Record<string, unknown>;
  if (typeof raw.id !== 'string' || raw.id.trim() === '') {
    throw new Error('Fan performance submission response was missing an id.');
  }

  if (typeof raw.status !== 'string' || raw.status.trim() === '') {
    throw new Error('Fan performance submission response was missing a status.');
  }

  if (typeof raw.title !== 'string') {
    throw new Error('Fan performance submission response was missing a title.');
  }

  if (typeof raw.submittedAt !== 'string' || raw.submittedAt.trim() === '') {
    throw new Error('Fan performance submission response was missing a submitted time.');
  }

  return {
    id: raw.id,
    status: raw.status,
    title: raw.title,
    submittedAt: raw.submittedAt,
  };
}
