import { sendMultipart } from './client';
import {
  parsePhotoSubmissionCreated,
  photoSubmissionFieldEntries,
  photoSubmissionsPath,
  type PhotoSubmissionFields,
  type PhotoUploadFile,
} from './photoSubmissionForm';
import type { PhotoSubmissionCreated } from './types';

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

  form.append('photo', {
    uri: input.photo.uri,
    name: input.photo.name,
    type: input.photo.type,
  } as unknown as Blob);

  return parsePhotoSubmissionCreated(
    await sendMultipart(photoSubmissionsPath, form, { accessToken, signal }),
  );
}
