import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { getNewsListEpoch, noteNewsListPush } from './newsListEpoch.ts';
import { bumpPmUnreadEpoch, getPmUnreadEpoch, notePmUnreadPush, subscribePmUnreadEpoch } from './pmUnreadEpoch.ts';

describe('pmUnreadEpoch', () => {
  it('starts at a numeric generation and increments only on bump', () => {
    const start = getPmUnreadEpoch();
    assert.equal(typeof start, 'number');
    bumpPmUnreadEpoch();
    assert.equal(getPmUnreadEpoch(), start + 1);
  });

  it('notifies current listeners and not ones that already unsubscribed', () => {
    const heard: number[] = [];
    const stop = subscribePmUnreadEpoch(() => {
      heard.push(getPmUnreadEpoch());
    });
    bumpPmUnreadEpoch();
    stop();
    const afterUnsubscribe = getPmUnreadEpoch();
    bumpPmUnreadEpoch();
    assert.deepEqual(heard, [afterUnsubscribe]);
    assert.equal(getPmUnreadEpoch(), afterUnsubscribe + 1);
  });

  it('bumps only for privateMessage destinations and leaves the news epoch alone', () => {
    const pmStart = getPmUnreadEpoch();
    const newsStart = getNewsListEpoch();
    notePmUnreadPush({ category: 'forumReply', topicId: 1002 });
    notePmUnreadPush({ category: 'news', articleId: 1003 });
    noteNewsListPush({
      category: 'privateMessage',
      conversationId: '11111111-2222-3333-4444-555555555555',
    });
    assert.equal(getPmUnreadEpoch(), pmStart);
    assert.equal(getNewsListEpoch(), newsStart);

    notePmUnreadPush({
      category: 'privateMessage',
      conversationId: '11111111-2222-3333-4444-555555555555',
    });
    assert.equal(getPmUnreadEpoch(), pmStart + 1);
    assert.equal(getNewsListEpoch(), newsStart);
  });
});
