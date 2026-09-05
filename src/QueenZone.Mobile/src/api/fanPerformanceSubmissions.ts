import { reportApiFailure } from '../config/sentry';
import { sendJson, sendMultipart } from './client';
import { isLocalFileFailure } from './errors';
import {
  fanPerformanceSubmissionFieldEntries,
  fanPerformanceSubmissionsPath,
  parseFanPerformanceSubmissionCreated,
  type AudioUploadFile,
  type FanPerformanceSubmissionCreated,
  type FanPerformanceSubmissionFields,
} from './fanPerformanceSubmissionForm';
import { appendUploadFile } from './uploadFile';

export {
  fanPerformanceSubmissionFieldEntries,
  fanPerformanceSubmissionsPath,
  parseFanPerformanceSubmissionCreated,
} from './fanPerformanceSubmissionForm';
export type {
  AudioUploadFile,
  FanPerformanceSubmissionCreated,
  FanPerformanceSubmissionFields,
} from './fanPerformanceSubmissionForm';

export type FanPerformanceSubmissionInput = FanPerformanceSubmissionFields & {
  audio: AudioUploadFile;
};

export type FanPerformanceReportCreated = {
  reportId: string;
  alreadyReported: boolean;
};

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
