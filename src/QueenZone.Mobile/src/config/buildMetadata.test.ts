import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { formatBuildStamp, formatHomeFooter, formatHomeFooterDate } from './buildMetadata.ts';

describe('formatHomeFooter', () => {
  it('shows version alone when the publish timestamp is unset', () => {
    assert.equal(formatHomeFooter({ version: '0.1.0' }), '0.1.0');
    assert.equal(formatHomeFooterDate(undefined), null);
    assert.equal(formatHomeFooterDate('not-a-date'), null);
  });

  it('adds the UTC calendar date from the baked timestamp', () => {
    assert.equal(formatHomeFooterDate('2026-08-29T13:40:12Z'), '29 Aug 2026');
    assert.equal(
      formatHomeFooter({
        version: '0.1.214',
        buildTimestampUtc: '2026-08-29T13:40:12Z',
      }),
      '0.1.214 · 29 Aug 2026',
    );
  });
});

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

    assert.match(
      result ?? '',
      /^Build 0\.1\.0 \(5\) · .+2026.+ · abc1234$/,
    );
  });

  it('stays hidden when build time is absent or invalid', () => {
    assert.equal(formatBuildStamp({ version: '0.1.0' }), null);
    assert.equal(
      formatBuildStamp({ version: '0.1.0', buildTimestampUtc: 'not-a-date' }),
      null,
    );
  });
});
