import { uploadMemberAvatar, memberAvatarPath } from './memberAvatar';
import { jsonResponse } from '../test/fixtures';
import { reportApiFailure } from '../config/sentry';

jest.mock('../config', () => ({
  apiV1Url: (path: string) => `http://qz.test/api/v1${path.startsWith('/') ? path : `/${path}`}`,
}));

jest.mock('../config/sentry', () => ({
  reportApiFailure: jest.fn(),
}));

const fetchMock = jest.fn<Promise<Response>, [RequestInfo | URL, RequestInit?]>();
const reportApiFailureMock = reportApiFailure as jest.MockedFunction<typeof reportApiFailure>;

beforeEach(() => {
  fetchMock.mockReset();
  reportApiFailureMock.mockReset();
  global.fetch = fetchMock as unknown as typeof fetch;
});

function jpegBlobResponse() {
  return new Response(new Uint8Array([0xff, 0xd8, 0xff]), {
    status: 200,
    headers: { 'Content-Type': 'image/jpeg' },
  });
}

const profilePayload = {
  memberId: '11111111-1111-1111-1111-111111111111',
  email: 'fan@example.com',
  displayName: 'Roger',
  hasAvatar: true,
  avatarPath: '/account/avatar/11111111-1111-1111-1111-111111111111',
};

describe('uploadMemberAvatar', () => {
  it('posts the picker file on /me/avatar', async () => {
    fetchMock.mockResolvedValueOnce(jpegBlobResponse()).mockResolvedValueOnce(jsonResponse(profilePayload));

    const profile = await uploadMemberAvatar(
      { uri: 'file:///tmp/avatar.jpg', name: 'avatar.jpg', type: 'image/jpeg' },
      'tok',
    );

    expect(memberAvatarPath).toBe('/me/avatar');
    expect(String(fetchMock.mock.calls[0]?.[0])).toBe('file:///tmp/avatar.jpg');
    const init = fetchMock.mock.calls[1]?.[1] ?? {};
    expect(String(fetchMock.mock.calls[1]?.[0])).toBe('http://qz.test/api/v1/me/avatar');
    expect(init.method).toBe('POST');
    expect(init.headers).toMatchObject({ Authorization: 'Bearer tok' });
    expect(init.headers).not.toHaveProperty('Content-Type');
    expect((init.body as FormData).get('file')).toBeInstanceOf(Blob);
    expect(profile.hasAvatar).toBe(true);
    expect(profile.displayName).toBe('Roger');
  });

  it('maps a failed local file read to local-file and reports it', async () => {
    const cause = new TypeError('Network request failed');
    fetchMock.mockRejectedValueOnce(cause);

    await expect(
      uploadMemberAvatar({ uri: 'file:///tmp/avatar.jpg', name: 'avatar.jpg', type: 'image/jpeg' }, 'tok'),
    ).rejects.toMatchObject({
      kind: 'local-file',
      message: 'Could not read the selected photo. Try choosing it again.',
    });

    expect(reportApiFailureMock).toHaveBeenCalledWith({
      kind: 'local-file',
      status: 0,
      method: 'POST',
      path: '/me/avatar',
      cause,
    });
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });
});
