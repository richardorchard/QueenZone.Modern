import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { fanPerformanceAudioPath, formatTrackDuration } from './formatDuration.ts';

describe('formatTrackDuration', () => {
  it('returns empty string for missing or invalid values', () => {
    assert.equal(formatTrackDuration(null), '');
    assert.equal(formatTrackDuration(undefined), '');
    assert.equal(formatTrackDuration(Number.NaN), '');
    assert.equal(formatTrackDuration(-1), '');
  });

  it('formats minutes and seconds', () => {
    assert.equal(formatTrackDuration(0), '0:00');
    assert.equal(formatTrackDuration(5), '0:05');
    assert.equal(formatTrackDuration(320), '5:20');
    assert.equal(formatTrackDuration(778), '12:58');
  });

  it('includes hours when needed', () => {
    assert.equal(formatTrackDuration(3600), '1:00:00');
    assert.equal(formatTrackDuration(3661), '1:01:01');
  });
});

describe('fanPerformanceAudioPath', () => {
  it('uses the member-gated content API stream', () => {
    assert.equal(fanPerformanceAudioPath(187), '/content/fan-performances/187/audio');
  });
});
