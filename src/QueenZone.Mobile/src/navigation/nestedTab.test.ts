import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { goBackOrFallback, leaveStoryScreen, nestedTabParams, storyLeaveFallback } from './nestedTab.ts';

describe('nestedTabParams', () => {
  it('keeps the tab root under a detail screen', () => {
    assert.deepEqual(nestedTabParams('Story', { id: 1003 }), {
      screen: 'Story',
      params: { id: 1003 },
      initial: false,
    });
    assert.deepEqual(nestedTabParams('Timeline'), { screen: 'Timeline', initial: false });
  });
});

describe('goBackOrFallback', () => {
  it('pops when the stack has history', () => {
    const navigation = {
      canGoBack: () => true,
      goBack: () => {
        calls.push('back');
      },
      navigate: (name: 'NewsIndex') => {
        calls.push(name);
      },
    };
    const calls: string[] = [];
    goBackOrFallback(navigation, 'NewsIndex');
    assert.deepEqual(calls, ['back']);
  });

  it('falls back to ArchiveHub, never Home, from a Timeline-only stack', () => {
    const calls: string[] = [];
    const navigation = {
      canGoBack: () => false,
      goBack: () => {
        calls.push('back');
      },
      navigate: (name: 'ArchiveHub') => {
        calls.push(name);
      },
    };
    goBackOrFallback(navigation, 'ArchiveHub');
    assert.deepEqual(calls, ['ArchiveHub']);
  });

  it('opens the tab root when the detail screen is the only route', () => {
    const navigation = {
      canGoBack: () => false,
      goBack: () => {
        calls.push('back');
      },
      navigate: (name: 'NewsIndex') => {
        calls.push(name);
      },
    };
    const calls: string[] = [];
    goBackOrFallback(navigation, 'NewsIndex');
    assert.deepEqual(calls, ['NewsIndex']);
  });
});

describe('storyLeaveFallback', () => {
  it('returns Home when the article was opened on the Home stack', () => {
    assert.equal(storyLeaveFallback(['Home', 'Story']), 'Home');
    assert.equal(storyLeaveFallback(['NewsIndex', 'Story']), 'NewsIndex');
    assert.equal(storyLeaveFallback(undefined), 'NewsIndex');
  });
});

describe('leaveStoryScreen', () => {
  it('returns to Home when a Home-stack story has no history', () => {
    const calls: string[] = [];
    leaveStoryScreen({
      canGoBack: () => false,
      goBack: () => {
        calls.push('back');
      },
      navigate: (name) => {
        calls.push(name);
      },
      getState: () => ({ routeNames: ['Home', 'Search', 'Story'] }),
    });
    assert.deepEqual(calls, ['Home']);
  });
});
