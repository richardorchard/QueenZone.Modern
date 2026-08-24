import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  completeSignInNavigation,
  openForumComposer,
  openPhotoSubmit,
  openSignIn,
  tabsNavigateParams,
} from './signInNavigation.ts';

function navigatePayload(action: unknown): { type?: string; payload?: { name?: string; params?: unknown } } {
  return action as { type?: string; payload?: { name?: string; params?: unknown } };
}

describe('tabsNavigateParams', () => {
  it('omits params when the destination has none', () => {
    assert.deepEqual(tabsNavigateParams({ tab: 'HomeTab', screen: 'Profile' }), {
      screen: 'HomeTab',
      params: { screen: 'Profile' },
    });
  });

  it('forwards composer params so login can return to the draft', () => {
    assert.deepEqual(
      tabsNavigateParams({
        tab: 'ForumTab',
        screen: 'Composer',
        params: { threadId: 9, threadTitle: 'News' },
      }),
      {
        screen: 'ForumTab',
        params: { screen: 'Composer', params: { threadId: 9, threadTitle: 'News' } },
      },
    );
  });
});

describe('completeSignInNavigation', () => {
  it('returns to the prompting screen when returnTo is set', () => {
    const navigation = {
      navigate: (name: 'Tabs', params: ReturnType<typeof tabsNavigateParams>) => {
        calls.push({ name, params });
      },
      goBack: () => {
        calls.push({ name: 'goBack' });
      },
      canGoBack: () => true,
    };
    const calls: unknown[] = [];
    completeSignInNavigation(navigation, { tab: 'PhotosTab', screen: 'PhotoSubmit' });
    assert.deepEqual(calls, [
      { name: 'Tabs', params: { screen: 'PhotosTab', params: { screen: 'PhotoSubmit' } } },
    ]);
  });

  it('dismisses the sign-in modal when there is no return target', () => {
    const calls: string[] = [];
    completeSignInNavigation(
      {
        navigate: () => {
          calls.push('navigate');
        },
        goBack: () => {
          calls.push('goBack');
        },
        canGoBack: () => true,
      },
      undefined,
    );
    assert.deepEqual(calls, ['goBack']);
  });

  it('falls back to Profile when the modal cannot pop', () => {
    const calls: unknown[] = [];
    completeSignInNavigation(
      {
        navigate: (name, params) => {
          calls.push({ name, params });
        },
        goBack: () => {
          calls.push('goBack');
        },
        canGoBack: () => false,
      },
      undefined,
    );
    assert.deepEqual(calls, [
      { name: 'Tabs', params: { screen: 'HomeTab', params: { screen: 'Profile' } } },
    ]);
  });
});

describe('openSignIn', () => {
  it('dispatches a root SignIn route so the sheet sits above tab modals', () => {
    const dispatched: unknown[] = [];
    openSignIn(
      {
        dispatch: (action) => {
          dispatched.push(action);
        },
      },
      { tab: 'HomeTab', screen: 'Profile' },
    );
    const action = navigatePayload(dispatched[0]);
    assert.equal(action.type, 'NAVIGATE');
    assert.equal(action.payload?.name, 'SignIn');
    assert.deepEqual(action.payload?.params, { returnTo: { tab: 'HomeTab', screen: 'Profile' } });
  });
});

describe('gated compose helpers', () => {
  it('opens Composer directly when already signed in', () => {
    const calls: unknown[] = [];
    openForumComposer(
      {
        navigate: (name, params) => {
          calls.push({ name, params });
        },
        dispatch: (action) => {
          calls.push(action);
        },
      },
      true,
      { threadId: 3 },
    );
    assert.deepEqual(calls, [{ name: 'Composer', params: { threadId: 3 } }]);
  });

  it('sends signed-out compose to SignIn with a Composer return', () => {
    const dispatched: unknown[] = [];
    openForumComposer(
      {
        navigate: () => {
          dispatched.push('composer');
        },
        dispatch: (action) => {
          dispatched.push(action);
        },
      },
      false,
      { categoryId: 2, categoryName: 'General' },
    );
    assert.equal(dispatched.length, 1);
    const action = navigatePayload(dispatched[0]);
    assert.equal(action.type, 'NAVIGATE');
    assert.equal(action.payload?.name, 'SignIn');
    assert.deepEqual(action.payload?.params, {
      returnTo: {
        tab: 'ForumTab',
        screen: 'Composer',
        params: { categoryId: 2, categoryName: 'General' },
      },
    });
  });

  it('sends signed-out photo submit to SignIn', () => {
    let submitted = false;
    const dispatched: unknown[] = [];
    openPhotoSubmit(
      {
        dispatch: (action) => {
          dispatched.push(action);
        },
      },
      false,
      () => {
        submitted = true;
      },
    );
    assert.equal(submitted, false);
    assert.equal(dispatched.length, 1);
  });
});
