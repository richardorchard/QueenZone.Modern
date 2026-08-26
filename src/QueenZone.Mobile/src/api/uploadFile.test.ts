import assert from 'node:assert/strict';
import { afterEach, describe, it } from 'node:test';
import { ApiError } from './errors.ts';
import { appendUploadFile, readUploadFileBlob } from './uploadFile.ts';

const originalFetch = globalThis.fetch;

afterEach(() => {
  globalThis.fetch = originalFetch;
});

function jpegResponse(bytes: Uint8Array = new Uint8Array([0xff, 0xd8, 0xff])): Response {
  return new Response(bytes, {
    status: 200,
    headers: { 'Content-Type': 'image/jpeg' },
  });
}

describe('readUploadFileBlob', () => {
  it('returns a typed blob from a local URI', async () => {
    globalThis.fetch = (async () => jpegResponse()) as typeof fetch;
    const blob = await readUploadFileBlob({
      uri: 'file:///tmp/photo.jpg',
      name: 'photo.jpg',
      type: 'image/jpeg',
    });
    assert.equal(blob.type, 'image/jpeg');
    assert.equal(blob.size, 3);
  });

  it('maps a fetch TypeError to local-file and keeps the cause', async () => {
    const cause = new TypeError('Network request failed');
    globalThis.fetch = (async () => {
      throw cause;
    }) as typeof fetch;
    await assert.rejects(
      () =>
        readUploadFileBlob({
          uri: 'file:///tmp/missing.jpg',
          name: 'missing.jpg',
          type: 'image/jpeg',
        }),
      (err: unknown) => {
        assert.ok(err instanceof ApiError);
        assert.equal(err.kind, 'local-file');
        assert.equal(err.cause, cause);
        return true;
      },
    );
  });

  it('rethrows AbortError', async () => {
    const abort = Object.assign(new Error('aborted'), { name: 'AbortError' });
    globalThis.fetch = (async () => {
      throw abort;
    }) as typeof fetch;
    await assert.rejects(
      () =>
        readUploadFileBlob({
          uri: 'file:///tmp/photo.jpg',
          name: 'photo.jpg',
          type: 'image/jpeg',
        }),
      (err: unknown) => err === abort,
    );
  });

  it('rejects an empty blob', async () => {
    globalThis.fetch = (async () => jpegResponse(new Uint8Array())) as typeof fetch;
    await assert.rejects(
      () =>
        readUploadFileBlob({
          uri: 'file:///tmp/empty.jpg',
          name: 'empty.jpg',
          type: 'image/jpeg',
        }),
      (err: unknown) => err instanceof ApiError && err.kind === 'local-file',
    );
  });

  it('maps a failed blob() read to local-file', async () => {
    const cause = new TypeError('Failed to fetch');
    globalThis.fetch = (async () =>
      ({
        ok: true,
        blob: async () => {
          throw cause;
        },
      }) as Response) as typeof fetch;
    await assert.rejects(
      () =>
        readUploadFileBlob({
          uri: 'file:///tmp/photo.jpg',
          name: 'photo.jpg',
          type: 'image/jpeg',
        }),
      (err: unknown) => err instanceof ApiError && err.kind === 'local-file' && err.cause === cause,
    );
  });

  it('rejects a non-OK local read', async () => {
    globalThis.fetch = (async () => new Response(null, { status: 404 })) as typeof fetch;
    await assert.rejects(
      () =>
        readUploadFileBlob({
          uri: 'file:///tmp/missing.jpg',
          name: 'missing.jpg',
          type: 'image/jpeg',
        }),
      (err: unknown) => err instanceof ApiError && err.kind === 'local-file',
    );
  });

  it('retags a blob when the picker MIME differs', async () => {
    globalThis.fetch = (async () =>
      new Response(new Uint8Array([0xff, 0xd8, 0xff]), {
        status: 200,
        headers: { 'Content-Type': 'application/octet-stream' },
      })) as typeof fetch;
    const blob = await readUploadFileBlob({
      uri: 'file:///tmp/photo.jpg',
      name: 'photo.jpg',
      type: 'image/jpeg',
    });
    assert.equal(blob.type, 'image/jpeg');
  });
});

describe('appendUploadFile', () => {
  it('appends a Blob part with the original filename', async () => {
    globalThis.fetch = (async () => jpegResponse()) as typeof fetch;
    const form = new FormData();
    await appendUploadFile(form, 'photo', {
      uri: 'file:///tmp/photo.jpg',
      name: 'crowd.jpg',
      type: 'image/jpeg',
    });
    const part = form.get('photo');
    assert.ok(part instanceof Blob);
    if (part instanceof File) {
      assert.equal(part.name, 'crowd.jpg');
    }
  });
});
