import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { fileLooksLikeHttpError, looksLikeHttpErrorPayload } from './audioBytes.ts';

describe('looksLikeHttpErrorPayload', () => {
  it('treats JSON and HTML prefixes as error bodies', () => {
    assert.equal(looksLikeHttpErrorPayload(new Uint8Array([0x7b, 0x22])), true);
    assert.equal(looksLikeHttpErrorPayload(new Uint8Array([0x20, 0x3c, 0x68])), true);
    assert.equal(looksLikeHttpErrorPayload(new Uint8Array([1, 2, 3, 4])), false);
    assert.equal(looksLikeHttpErrorPayload(new Uint8Array([0x49, 0x44, 0x33])), false);
  });
});

describe('fileLooksLikeHttpError', () => {
  it('rejects empty files and sniffs only small payloads', async () => {
    assert.equal(
      await fileLooksLikeHttpError(async () => new Uint8Array([0x7b]), 'file:///x', 0),
      true,
    );
    assert.equal(
      await fileLooksLikeHttpError(async () => new Uint8Array([0x7b]), 'file:///x', 200),
      true,
    );
    assert.equal(
      await fileLooksLikeHttpError(async () => new Uint8Array([0x7b]), 'file:///x', 9000),
      false,
    );
  });
});
