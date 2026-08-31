import { Linking, Share } from 'react-native';
import {
  fetchForumAttachment,
  isCookieGatedForumAttachmentPath,
  openForumAttachmentFile,
  openForumAttachmentImage,
} from './forumAttachment';

jest.mock('../config', () => ({
  getAppConfig: () => ({ apiBaseUrl: 'http://qz.test' }),
  apiV1Url: (path: string) => `http://qz.test/api/v1${path.startsWith('/') ? path : `/${path}`}`,
}));

const fetchMock = jest.fn<Promise<Response>, [RequestInfo | URL, RequestInit?]>();

beforeEach(() => {
  fetchMock.mockReset();
  global.fetch = fetchMock as unknown as typeof fetch;
  jest.spyOn(Linking, 'openURL').mockResolvedValue(undefined);
  jest.spyOn(Share, 'share').mockResolvedValue({ action: Share.sharedAction });
});

afterEach(() => {
  jest.restoreAllMocks();
});

function okResponse(contentType: string, bytes: number[], url: string): Response {
  return {
    ok: true,
    status: 200,
    url,
    headers: {
      get: (name: string) => (name.toLowerCase() === 'content-type' ? contentType : null),
    },
    arrayBuffer: async () => Uint8Array.from(bytes).buffer,
  } as unknown as Response;
}

describe('isCookieGatedForumAttachmentPath', () => {
  it('rejects the website cookie path and allows the Bearer alias', () => {
    expect(isCookieGatedForumAttachmentPath('/forum/attachment/legacy/1002')).toBe(true);
    expect(
      isCookieGatedForumAttachmentPath('/forum/attachment/9001/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'),
    ).toBe(true);
    expect(isCookieGatedForumAttachmentPath('/api/v1/forum/attachments/legacy/1002')).toBe(false);
    expect(isCookieGatedForumAttachmentPath('https://cdn2.queenzone.org/attachments/scan.jpg')).toBe(
      false,
    );
  });
});

describe('fetchForumAttachment', () => {
  it('refuses the cookie-gated path', async () => {
    await expect(fetchForumAttachment('/forum/attachment/legacy/1002', 'tok')).rejects.toMatchObject({
      name: 'ApiError',
      status: 400,
    });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('sends Bearer and returns the fetched bytes', async () => {
    fetchMock.mockResolvedValueOnce(okResponse('image/jpeg', [1, 2, 3], 'https://cdn2.queenzone.org/attachments/scan.jpg'));

    const result = await fetchForumAttachment('/api/v1/forum/attachments/legacy/1002', 'tok');
    expect(fetchMock).toHaveBeenCalledWith(
      'http://qz.test/api/v1/forum/attachments/legacy/1002',
      expect.objectContaining({
        method: 'GET',
        redirect: 'follow',
        headers: expect.objectContaining({ Authorization: 'Bearer tok' }),
      }),
    );
    expect(result.contentType).toBe('image/jpeg');
    expect(result.dataUri.startsWith('data:image/jpeg;base64,')).toBe(true);
  });

  it('maps 401 to a sign-in error', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: false,
      status: 401,
      url: 'http://qz.test/api/v1/forum/attachments/legacy/1002',
      headers: { get: () => null },
      arrayBuffer: async () => new ArrayBuffer(0),
    } as unknown as Response);
    await expect(fetchForumAttachment('/api/v1/forum/attachments/legacy/1002', 'tok')).rejects.toMatchObject({
      name: 'ApiError',
      status: 401,
      message: 'Sign in to continue.',
    });
  });
});

describe('openForumAttachmentFile', () => {
  it('opens a public CDN redirect after the Bearer fetch', async () => {
    fetchMock.mockResolvedValueOnce(
      okResponse('application/pdf', [1], 'https://cdn2.queenzone.org/attachments/notes.pdf'),
    );

    await openForumAttachmentFile('/api/v1/forum/attachments/legacy/1101', 'tok', 'notes.pdf');
    expect(Linking.openURL).toHaveBeenCalledWith('https://cdn2.queenzone.org/attachments/notes.pdf');
    expect(Share.share).not.toHaveBeenCalled();
  });

  it('opens a sound-file CDN redirect after the Bearer fetch', async () => {
    fetchMock.mockResolvedValueOnce(
      okResponse('audio/mpeg', [1], 'https://cdn2.queenzone.org/attachments/solo.mp3'),
    );

    await openForumAttachmentFile('/api/v1/forum/attachments/legacy/1201', 'tok', 'solo.mp3');
    expect(fetchMock).toHaveBeenCalledWith(
      'http://qz.test/api/v1/forum/attachments/legacy/1201',
      expect.objectContaining({
        method: 'GET',
        redirect: 'follow',
        headers: expect.objectContaining({ Authorization: 'Bearer tok' }),
      }),
    );
    expect(Linking.openURL).toHaveBeenCalledWith('https://cdn2.queenzone.org/attachments/solo.mp3');
    expect(Share.share).not.toHaveBeenCalled();
  });

  it('shares streamed bytes when the API does not redirect', async () => {
    fetchMock.mockResolvedValueOnce(
      okResponse(
        'text/plain',
        [9, 8],
        'http://qz.test/api/v1/forum/attachments/9001/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
      ),
    );

    await openForumAttachmentFile(
      '/api/v1/forum/attachments/9001/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
      'tok',
      'notes.txt',
    );
    expect(Linking.openURL).not.toHaveBeenCalled();
    expect(Share.share).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'notes.txt', url: expect.stringMatching(/^data:text\/plain;base64,/) }),
    );
  });

  it('skips the OEM share sheet when present is false', async () => {
    fetchMock.mockResolvedValueOnce(
      okResponse(
        'text/plain',
        [9, 8],
        'http://qz.test/api/v1/forum/attachments/9001/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
      ),
    );

    await openForumAttachmentFile(
      '/api/v1/forum/attachments/9001/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
      'tok',
      'attach.txt',
      { present: false },
    );
    expect(Linking.openURL).not.toHaveBeenCalled();
    expect(Share.share).not.toHaveBeenCalled();
  });
});

describe('openForumAttachmentImage', () => {
  it('refuses the cookie-gated path', async () => {
    await expect(openForumAttachmentImage('/forum/attachment/legacy/1002', 'tok')).rejects.toMatchObject({
      name: 'ApiError',
      status: 400,
    });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('follows the legacy redirect and returns a cached data URI', async () => {
    fetchMock.mockResolvedValue(
      okResponse('image/jpeg', [1, 2, 3], 'https://cdn2.queenzone.org/attachments/scan.jpg'),
    );

    const first = await openForumAttachmentImage('/api/v1/forum/attachments/legacy/1043-scan', 'tok');
    const second = await openForumAttachmentImage('/api/v1/forum/attachments/legacy/1043-scan', 'tok');

    expect(first).toMatch(/^data:image\/jpeg;base64,/);
    expect(second).toBe(first);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(fetchMock).toHaveBeenCalledWith(
      'http://qz.test/api/v1/forum/attachments/legacy/1043-scan',
      expect.objectContaining({ redirect: 'follow' }),
    );
  });

  it('refuses a cookie-gated final URL after follow', async () => {
    fetchMock.mockResolvedValueOnce(
      okResponse('image/jpeg', [1], 'http://qz.test/forum/attachment/legacy/1002'),
    );

    await expect(
      openForumAttachmentImage('/api/v1/forum/attachments/legacy/1043-cookie-final', 'tok'),
    ).rejects.toMatchObject({
      name: 'ApiError',
      status: 400,
    });
  });
});
