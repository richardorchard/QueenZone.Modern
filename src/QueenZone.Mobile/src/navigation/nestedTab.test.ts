import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { goBackOrFallback, nestedTabParams } from './nestedTab.ts';

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
