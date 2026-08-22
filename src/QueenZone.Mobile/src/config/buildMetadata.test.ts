import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { formatBuildStamp } from './buildMetadata.ts';

describe('formatBuildStamp', () => {
  it('formats version, build, local timestamp, and revision', () => {
    const result = formatBuildStamp(
      {
        version: '0.1.0',
        buildNumber: '5',
        buildTimestampUtc: '2026-08-22T03:04:00Z',
        buildRevision: 'abc1234',
      },
      'en-AU',
    );

    assert.match(result ?? '', /^Build 0\.1\.0 \(5\) · 22 Aug 2026,/);
    assert.match(result ?? '', / · abc1234$/);
  });

  it('stays hidden when build time is absent or invalid', () => {
    assert.equal(formatBuildStamp({ version: '0.1.0' }), null);
    assert.equal(
      formatBuildStamp({ version: '0.1.0', buildTimestampUtc: 'not-a-date' }),
      null,
    );
  });
});
