import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  fanPerformanceSubmissionFieldEntries,
  fanPerformanceSubmissionsPath,
  parseFanPerformanceSubmissionCreated,
} from './fanPerformanceSubmissionForm.ts';

describe('fanPerformanceSubmissionsPath', () => {
  it('posts under the versioned member API', () => {
    assert.equal(fanPerformanceSubmissionsPath, '/member/fan-performance-submissions');
  });
});

describe('fanPerformanceSubmissionFieldEntries', () => {
  it('sends the same field names as the website multipart form', () => {
    assert.deepEqual(
      fanPerformanceSubmissionFieldEntries({
        title: '  Reaching Out cover  ',
        coveredSong: ' Reaching Out ',
        performedBy: ' Stage Fan ',
        rightsDeclarationAccepted: true,
      }),
      [
        ['title', 'Reaching Out cover'],
        ['coveredSong', 'Reaching Out'],
        ['performedBy', 'Stage Fan'],
        ['rightsDeclarationAccepted', 'true'],
      ],
    );
  });

  it('includes a trimmed description and records a declined rights flag', () => {
    assert.deepEqual(
      fanPerformanceSubmissionFieldEntries({
        title: 'Cover',
        coveredSong: 'Liar',
        performedBy: 'Fan',
        description: '  Studio take  ',
        rightsDeclarationAccepted: false,
      }),
      [
        ['title', 'Cover'],
        ['coveredSong', 'Liar'],
        ['performedBy', 'Fan'],
        ['rightsDeclarationAccepted', 'false'],
        ['description', 'Studio take'],
      ],
    );
  });
});

describe('parseFanPerformanceSubmissionCreated', () => {
  it('reads the 201 contract', () => {
    const created = parseFanPerformanceSubmissionCreated({
      id: '11111111-1111-1111-1111-111111111111',
      status: 'Pending',
      title: 'Reaching Out cover',
      submittedAt: '2026-09-04T00:15:00.000Z',
    });
    assert.equal(created.status, 'Pending');
    assert.equal(created.title, 'Reaching Out cover');
    assert.equal(created.submittedAt, '2026-09-04T00:15:00.000Z');
  });

  it('rejects payloads that cannot show confirmation', () => {
    assert.throws(() => parseFanPerformanceSubmissionCreated(null), /empty/);
    assert.throws(() => parseFanPerformanceSubmissionCreated({ title: 'Missing id' }), /id/);
    assert.throws(
      () => parseFanPerformanceSubmissionCreated({ id: '1', title: 'x', submittedAt: 't' }),
      /status/,
    );
    assert.throws(
      () => parseFanPerformanceSubmissionCreated({ id: '1', status: 'Pending', submittedAt: 't' }),
      /title/,
    );
    assert.throws(
      () => parseFanPerformanceSubmissionCreated({ id: '1', status: 'Pending', title: 'x' }),
      /submitted time/,
    );
  });
});
