import { ApiError } from './errors';
import { appendNativeUploadFile, appendUploadFile, readUploadFileBlob } from './uploadFile';

const fetchMock = jest.fn<Promise<Response>, [RequestInfo | URL, RequestInit?]>();

beforeEach(() => {
  fetchMock.mockReset();
  global.fetch = fetchMock as unknown as typeof fetch;
});

function jpegResponse(bytes: Uint8Array = new Uint8Array([0xff, 0xd8, 0xff])): Response {
  return new Response(bytes, {
    status: 200,
    headers: { 'Content-Type': 'image/jpeg' },
  });
}

describe('readUploadFileBlob', () => {
  it('returns a typed blob from a local URI', async () => {
    fetchMock.mockResolvedValueOnce(jpegResponse());
    const blob = await readUploadFileBlob({
      uri: 'file:///tmp/photo.jpg',
      name: 'photo.jpg',
      type: 'image/jpeg',
    });
    expect(blob.type).toBe('image/jpeg');
    expect(blob.size).toBe(3);
  });

  it('maps a fetch TypeError to local-file and keeps the cause', async () => {
    const cause = new TypeError('Network request failed');
    fetchMock.mockRejectedValueOnce(cause);
    await expect(
      readUploadFileBlob({
        uri: 'file:///tmp/missing.jpg',
        name: 'missing.jpg',
        type: 'image/jpeg',
      }),
    ).rejects.toMatchObject({
      kind: 'local-file',
      cause,
    });
  });

  it('rethrows AbortError', async () => {
    const abort = Object.assign(new Error('aborted'), { name: 'AbortError' });
    fetchMock.mockRejectedValueOnce(abort);
    await expect(
      readUploadFileBlob({
        uri: 'file:///tmp/photo.jpg',
        name: 'photo.jpg',
        type: 'image/jpeg',
      }),
    ).rejects.toBe(abort);
  });

  it('rejects an empty blob', async () => {
    fetchMock.mockResolvedValueOnce(jpegResponse(new Uint8Array()));
    await expect(
      readUploadFileBlob({
        uri: 'file:///tmp/empty.jpg',
        name: 'empty.jpg',
        type: 'image/jpeg',
      }),
    ).rejects.toMatchObject({ kind: 'local-file' });
  });

  it('rethrows AbortError from blob()', async () => {
    const abort = Object.assign(new Error('aborted'), { name: 'AbortError' });
    fetchMock.mockResolvedValueOnce({
      ok: true,
      blob: async () => {
        throw abort;
      },
    } as Response);
    await expect(
      readUploadFileBlob({
        uri: 'file:///tmp/photo.jpg',
        name: 'photo.jpg',
        type: 'image/jpeg',
      }),
    ).rejects.toBe(abort);
  });

  it('maps a failed blob() read to local-file', async () => {
    const cause = new TypeError('Failed to fetch');
    fetchMock.mockResolvedValueOnce({
      ok: true,
      blob: async () => {
        throw cause;
      },
    } as Response);
    await expect(
      readUploadFileBlob({
        uri: 'file:///tmp/photo.jpg',
        name: 'photo.jpg',
        type: 'image/jpeg',
      }),
    ).rejects.toMatchObject({ kind: 'local-file', cause });
  });

  it('rejects a non-OK local read', async () => {
    fetchMock.mockResolvedValueOnce(new Response(null, { status: 404 }));
    await expect(
      readUploadFileBlob({
        uri: 'file:///tmp/missing.jpg',
        name: 'missing.jpg',
        type: 'image/jpeg',
      }),
    ).rejects.toBeInstanceOf(ApiError);
  });

  it('retags a blob when the picker MIME differs', async () => {
    fetchMock.mockResolvedValueOnce(
      new Response(new Uint8Array([0xff, 0xd8, 0xff]), {
        status: 200,
        headers: { 'Content-Type': 'application/octet-stream' },
      }),
    );
    const blob = await readUploadFileBlob({
      uri: 'file:///tmp/photo.jpg',
      name: 'photo.jpg',
      type: 'image/jpeg',
    });
    expect(blob.type).toBe('image/jpeg');
  });
});

describe('appendUploadFile', () => {
  it('appends a Blob part with the original filename', async () => {
    fetchMock.mockResolvedValueOnce(jpegResponse());
    const form = new FormData();
    await appendUploadFile(form, 'photo', {
      uri: 'file:///tmp/photo.jpg',
      name: 'crowd.jpg',
      type: 'image/jpeg',
    });
    const part = form.get('photo');
    expect(part).toBeInstanceOf(Blob);
    if (part instanceof File) {
      expect(part.name).toBe('crowd.jpg');
    }
  });
});

describe('appendNativeUploadFile', () => {
  it('appends the React Native { uri, name, type } file part', () => {
    const append = jest.fn();
    const form = { append } as unknown as FormData;
    appendNativeUploadFile(form, 'file', {
      uri: 'file:///tmp/avatar.jpg',
      name: 'avatar.jpg',
      type: 'image/jpeg',
    });
    expect(append).toHaveBeenCalledWith('file', {
      uri: 'file:///tmp/avatar.jpg',
      name: 'avatar.jpg',
      type: 'image/jpeg',
    });
  });
});
