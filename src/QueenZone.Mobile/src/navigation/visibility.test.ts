import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  getVisibleTabNames,
  isMemberOnlyScreen,
  publicTabNames,
  shouldHideTabBar,
  signedInOnlyTabNames,
} from './visibility.ts';

describe('getVisibleTabNames', () => {
  it('shows only public tabs when signed out', () => {
    assert.deepEqual([...getVisibleTabNames(false)], [...publicTabNames]);
    assert.equal(getVisibleTabNames(false).includes('MessagesTab'), false);
  });

  it('unlocks the messages tab when signed in, before You', () => {
    const tabs = getVisibleTabNames(true);
    assert.deepEqual(
      [...tabs],
      ['TodayTab', 'NewsTab', 'PhotosTab', 'ForumTab', 'MessagesTab', 'YouTab'],
    );
    assert.equal(tabs.includes(signedInOnlyTabNames[0]), true);
  });
});

describe('member-only screens', () => {
  it('gates private messages, compose, and account settings', () => {
    assert.equal(isMemberOnlyScreen('FanPerformanceDetail'), true);
    assert.equal(isMemberOnlyScreen('Inbox'), true);
    assert.equal(isMemberOnlyScreen('Conversation'), true);
    assert.equal(isMemberOnlyScreen('Composer'), true);
    assert.equal(isMemberOnlyScreen('Settings'), true);
    assert.equal(isMemberOnlyScreen('PhotoSubmit'), true);
  });

  it('keeps archive, photos, forum browse, and help public', () => {
    assert.equal(isMemberOnlyScreen('Today'), false);
    assert.equal(isMemberOnlyScreen('NewsIndex'), false);
    assert.equal(isMemberOnlyScreen('PhotoIndex'), false);
    assert.equal(isMemberOnlyScreen('ForumIndex'), false);
    assert.equal(isMemberOnlyScreen('FanPerformances'), false);
    assert.equal(isMemberOnlyScreen('Help'), false);
    assert.equal(isMemberOnlyScreen('Account'), false);
  });
});

describe('shouldHideTabBar', () => {
  it('hides the tab bar on pushed detail routes', () => {
    assert.equal(shouldHideTabBar('Story'), true);
    assert.equal(shouldHideTabBar('BiographyChapter'), true);
    assert.equal(shouldHideTabBar('Album'), true);
    assert.equal(shouldHideTabBar('Thread'), true);
    assert.equal(shouldHideTabBar('PhotoViewer'), true);
    assert.equal(shouldHideTabBar('Today'), false);
    assert.equal(shouldHideTabBar('NewsIndex'), false);
    assert.equal(shouldHideTabBar('Biography'), false);
  });
});
