import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { formatByteSize } from './formatBytes.ts';

describe('formatByteSize', () => {
  it('hides missing sizes and formats bytes through megabytes', () => {
    assert.equal(formatByteSize(null), '');
    assert.equal(formatByteSize(-1), '');
    assert.equal(formatByteSize(512), '512 B');
    assert.equal(formatByteSize(1536), '1.5 KB');
    assert.equal(formatByteSize(20 * 1024), '20 KB');
    assert.equal(formatByteSize(2.4 * 1024 * 1024), '2.4 MB');
  });
});
