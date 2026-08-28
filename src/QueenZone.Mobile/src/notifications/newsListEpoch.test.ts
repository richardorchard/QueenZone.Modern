import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { bumpNewsListEpoch, getNewsListEpoch, noteNewsListPush, subscribeNewsListEpoch } from './newsListEpoch.ts';

describe('newsListEpoch', () => {
  it('starts at a numeric generation and increments only on bump', () => {
    const start = getNewsListEpoch();
    assert.equal(typeof start, 'number');
    bumpNewsListEpoch();
    assert.equal(getNewsListEpoch(), start + 1);
  });

  it('notifies current listeners and not ones that already unsubscribed', () => {
    const heard: number[] = [];
    const stop = subscribeNewsListEpoch(() => {
      heard.push(getNewsListEpoch());
    });
    bumpNewsListEpoch();
    stop();
    const afterUnsubscribe = getNewsListEpoch();
    bumpNewsListEpoch();
    assert.deepEqual(heard, [afterUnsubscribe]);
    assert.equal(getNewsListEpoch(), afterUnsubscribe + 1);
  });

  it('bumps only for news destinations', () => {
    const start = getNewsListEpoch();
    noteNewsListPush({ category: 'forumReply', topicId: 1002 });
    noteNewsListPush({
      category: 'privateMessage',
      conversationId: '11111111-2222-3333-4444-555555555555',
    });
    assert.equal(getNewsListEpoch(), start);

    noteNewsListPush({ category: 'news', articleId: 1003 });
    assert.equal(getNewsListEpoch(), start + 1);
    noteNewsListPush({ category: 'news' });
    assert.equal(getNewsListEpoch(), start + 2);
  });
});
