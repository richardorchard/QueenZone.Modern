import { reportApiFailure } from '../config/sentry';
import { sendMultipart } from './client';
import { isLocalFileFailure } from './errors';
import { parseMemberProfile, type MemberProfile } from './me';
import { appendUploadFile, type UploadFilePart } from './uploadFile';

/** Authenticated `POST /api/v1/me/avatar` (issues #752 / #754). */
export const memberAvatarPath = '/me/avatar';

export async function uploadMemberAvatar(
  file: UploadFilePart,
  accessToken: string,
  signal?: AbortSignal,
): Promise<MemberProfile> {
  const form = new FormData();

  try {
    await appendUploadFile(form, 'file', file, signal);
  } catch (err) {
    if (isLocalFileFailure(err)) {
      reportApiFailure({
        kind: err.kind,
        status: err.status,
        method: 'POST',
        path: memberAvatarPath,
        cause: err.cause,
      });
    }
    throw err;
  }

  return parseMemberProfile(await sendMultipart(memberAvatarPath, form, { accessToken, signal }));
}
