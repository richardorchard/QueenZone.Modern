import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { composerCopy, composerMode, validateComposer } from './composerMeta.ts';

describe('composerMode', () => {
  it('treats a thread id as reply and everything else as a new topic', () => {
    assert.equal(composerMode({ threadId: 1002 }), 'reply');
    assert.equal(composerMode({ categoryId: 1 }), 'newTopic');
    assert.equal(composerMode({}), 'newTopic');
    assert.equal(composerMode(undefined), 'newTopic');
  });
});

describe('validateComposer', () => {
  it('requires a board and 5-200 character title for new topics', () => {
    assert.equal(
      validateComposer({ mode: 'newTopic', title: 'Hey', body: 'Hello fans', categoryId: 1 }),
      'Title must be between 5 and 200 characters.',
    );
    assert.equal(
      validateComposer({ mode: 'newTopic', title: 'Fresh forum news', body: 'Hello fans' }),
      'Choose a board for this topic.',
    );
    assert.equal(
      validateComposer({
        mode: 'newTopic',
        title: 'Fresh forum news',
        body: 'Hello fans',
        categoryId: 1,
      }),
      null,
    );
  });

  it('requires a body for replies and does not require a title', () => {
    assert.equal(
      validateComposer({ mode: 'reply', title: '', body: '   ' }),
      'Write a post before publishing.',
    );
    assert.equal(validateComposer({ mode: 'reply', title: '', body: 'A reply' }), null);
  });
});

describe('composerCopy', () => {
  it('labels reply vs new topic actions', () => {
    assert.deepEqual(composerCopy('reply'), { title: 'Reply', action: 'Post reply' });
    assert.deepEqual(composerCopy('newTopic'), { title: 'New topic', action: 'Post topic' });
  });
});
