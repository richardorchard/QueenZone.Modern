import { reportApiFailure } from '../config/sentry';
import { sendJson, sendMultipart } from './client';
import { isLocalFileFailure } from './errors';
import { appendUploadFile } from './uploadFile';

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

export type FanPerformanceSubmissionInput = FanPerformanceSubmissionFields & {
  audio: AudioUploadFile;
};

export type FanPerformanceSubmissionCreated = {
  id: string;
  status: string;
  title: string;
  submittedAt: string;
};

export type FanPerformanceReportCreated = {
  reportId: string;
  alreadyReported: boolean;
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

export async function createFanPerformanceSubmission(
  input: FanPerformanceSubmissionInput,
  accessToken: string,
  signal?: AbortSignal,
): Promise<FanPerformanceSubmissionCreated> {
  const form = new FormData();
  for (const [name, value] of fanPerformanceSubmissionFieldEntries(input)) {
    form.append(name, value);
  }

  try {
    await appendUploadFile(form, 'audio', input.audio, signal);
  } catch (err) {
    if (isLocalFileFailure(err)) {
      reportApiFailure({
        kind: err.kind,
        status: err.status,
        method: 'POST',
        path: fanPerformanceSubmissionsPath,
        cause: err.cause,
      });
    }
    throw err;
  }

  return parseFanPerformanceSubmissionCreated(
    await sendMultipart(fanPerformanceSubmissionsPath, form, { accessToken, signal }),
  );
}

export async function reportFanPerformance(
  stageId: number,
  reason: string,
  accessToken: string,
  signal?: AbortSignal,
): Promise<FanPerformanceReportCreated> {
  const payload = await sendJson<{ reportId: string; alreadyReported: boolean }>(
    `/me/fan-performances/${stageId}/report`,
    {
      accessToken,
      signal,
      body: { reason },
    },
  );
  if (typeof payload.reportId !== 'string' || payload.reportId.trim() === '') {
    throw new Error('Report response was missing an id.');
  }

  return {
    reportId: payload.reportId,
    alreadyReported: payload.alreadyReported === true,
  };
}
