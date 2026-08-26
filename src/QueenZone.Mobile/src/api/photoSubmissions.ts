import { reportApiFailure } from '../config/sentry';
import { sendMultipart } from './client';
import { isLocalFileFailure } from './errors';
import {
  parsePhotoSubmissionCreated,
  photoSubmissionFieldEntries,
  photoSubmissionsPath,
  type PhotoSubmissionFields,
  type PhotoUploadFile,
} from './photoSubmissionForm';
import type { PhotoSubmissionCreated } from './types';
import { appendUploadFile } from './uploadFile';

export {
  parsePhotoSubmissionCreated,
  photoSubmissionFieldEntries,
  photoSubmissionsPath,
} from './photoSubmissionForm';
export type { PhotoSubmissionFields, PhotoUploadFile } from './photoSubmissionForm';

export type PhotoSubmissionInput = PhotoSubmissionFields & {
  photo: PhotoUploadFile;
};

export async function createPhotoSubmission(
  input: PhotoSubmissionInput,
  accessToken: string,
  signal?: AbortSignal,
): Promise<PhotoSubmissionCreated> {
  const form = new FormData();
  for (const [name, value] of photoSubmissionFieldEntries(input)) {
    form.append(name, value);
  }

  try {
    await appendUploadFile(form, 'photo', input.photo, signal);
  } catch (err) {
    if (isLocalFileFailure(err)) {
      reportApiFailure({
        kind: err.kind,
        status: err.status,
        method: 'POST',
        path: photoSubmissionsPath,
        cause: err.cause,
      });
    }
    throw err;
  }

  return parsePhotoSubmissionCreated(
    await sendMultipart(photoSubmissionsPath, form, { accessToken, signal }),
  );
}
