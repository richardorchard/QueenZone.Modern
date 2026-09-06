import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { formatByteSize, formatDownloadProgress } from './formatBytes.ts';

describe('formatDownloadProgress', () => {
  it('prefers a percent when the total is known', () => {
    assert.equal(formatDownloadProgress(512, 1024), '50%');
    assert.equal(formatDownloadProgress(1024, 1024), '100%');
    assert.equal(formatDownloadProgress(2048, null), '2.0 KB');
    assert.equal(formatDownloadProgress(null, 1024), '');
  });
});

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
