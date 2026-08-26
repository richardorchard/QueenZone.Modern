import { createPhotoSubmission } from './photoSubmissions';
import { jsonResponse } from '../test/fixtures';

jest.mock('../config', () => ({
  apiV1Url: (path: string) => `http://qz.test/api/v1${path.startsWith('/') ? path : `/${path}`}`,
}));

const fetchMock = jest.fn<Promise<Response>, [RequestInfo | URL, RequestInit?]>();

beforeEach(() => {
  fetchMock.mockReset();
  global.fetch = fetchMock as unknown as typeof fetch;
});

function lastCall() {
  const call = fetchMock.mock.calls.at(-1);
  if (!call) {
    throw new Error('fetch was not called');
  }
  return { url: String(call[0]), init: call[1] ?? {} };
}

function jpegBlobResponse() {
  return new Response(new Uint8Array([0xff, 0xd8, 0xff]), {
    status: 200,
    headers: { 'Content-Type': 'image/jpeg' },
  });
}

describe('createPhotoSubmission', () => {
  it('reads the local photo as a blob then posts multipart with a Bearer token', async () => {
    fetchMock
      .mockResolvedValueOnce(jpegBlobResponse())
      .mockResolvedValueOnce(
        jsonResponse({ id: 'sub-1', status: 'pending', title: 'Wembley crowd', submittedAt: '2026-08-01T12:00:00Z' }),
      );

    const created = await createPhotoSubmission(
      {
        title: 'Wembley crowd',
        description: 'Live at Wembley',
        photo: { uri: 'file:///tmp/photo.jpg', name: 'photo.jpg', type: 'image/jpeg' },
      },
      'tok',
    );

    expect(String(fetchMock.mock.calls[0]?.[0])).toBe('file:///tmp/photo.jpg');
    const { url, init } = lastCall();
    expect(url).toBe('http://qz.test/api/v1/member/photo-submissions');
    expect(init.method).toBe('POST');
    expect(init.headers).toMatchObject({ Authorization: 'Bearer tok' });
    expect(init.headers).not.toHaveProperty('Content-Type');

    const form = init.body as FormData;
    expect(form.get('title')).toBe('Wembley crowd');
    expect(form.get('description')).toBe('Live at Wembley');
    expect(form.get('photo')).toBeInstanceOf(Blob);

    expect(created).toEqual({
      id: 'sub-1',
      status: 'pending',
      title: 'Wembley crowd',
      submittedAt: '2026-08-01T12:00:00Z',
    });
  });

  it('maps a failed local file read to local-file instead of offline', async () => {
    fetchMock.mockRejectedValueOnce(new TypeError('Network request failed'));
    await expect(
      createPhotoSubmission(
        { title: 'x', photo: { uri: 'file:///x.jpg', name: 'x.jpg', type: 'image/jpeg' } },
        'tok',
      ),
    ).rejects.toMatchObject({
      kind: 'local-file',
      message: 'Could not read the selected photo. Try choosing it again.',
    });
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('rejects a response missing required fields', async () => {
    fetchMock.mockResolvedValueOnce(jpegBlobResponse()).mockResolvedValueOnce(jsonResponse({ id: 'sub-1' }));
    await expect(
      createPhotoSubmission(
        { title: 'x', photo: { uri: 'file:///x.jpg', name: 'x.jpg', type: 'image/jpeg' } },
        'tok',
      ),
    ).rejects.toThrow('Photo submission response was missing a status.');
  });
});
