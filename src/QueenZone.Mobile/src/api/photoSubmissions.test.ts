import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  parsePhotoSubmissionCreated,
  photoSubmissionFieldEntries,
  photoSubmissionsPath,
} from './photoSubmissionForm.ts';

describe('photoSubmissionsPath', () => {
  it('posts under the versioned member API', () => {
    assert.equal(photoSubmissionsPath, '/member/photo-submissions');
  });
});

describe('photoSubmissionFieldEntries', () => {
  it('always sends title and omits blank optional fields', () => {
    assert.deepEqual(photoSubmissionFieldEntries({ title: '  Wembley crowd  ' }), [['title', 'Wembley crowd']]);
  });

  it('includes the same optional metadata names as /submit/photo', () => {
    assert.deepEqual(
      photoSubmissionFieldEntries({
        title: 'Wembley crowd shot',
        description: 'From the stands',
        suggestedCategory: 'Queen',
        approximateYear: 1986,
        approximateDate: '1986-07-12',
      }),
      [
        ['title', 'Wembley crowd shot'],
        ['description', 'From the stands'],
        ['suggestedCategory', 'Queen'],
        ['approximateYear', '1986'],
        ['approximateDate', '1986-07-12'],
      ],
    );
  });
});

describe('parsePhotoSubmissionCreated', () => {
  it('reads the 201 contract', () => {
    const created = parsePhotoSubmissionCreated({
      id: '11111111-1111-1111-1111-111111111111',
      status: 'Pending',
      title: 'Wembley crowd shot',
      submittedAt: '2026-08-23T00:15:00.000Z',
    });

    assert.equal(created.status, 'Pending');
    assert.equal(created.title, 'Wembley crowd shot');
  });

  it('rejects payloads that cannot show confirmation', () => {
    assert.throws(() => parsePhotoSubmissionCreated({ title: 'Missing id' }), /id/);
    assert.throws(() => parsePhotoSubmissionCreated(null), /empty/);
  });
});
