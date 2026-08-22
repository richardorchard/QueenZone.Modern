import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { ARCHIVE_HUB_IDS } from '../content/archiveHub.ts';
import {
  getVisibleTabNames,
  isMemberOnlyScreen,
  publicTabNames,
  shouldHideTabBar,
  signedInOnlyTabNames,
} from './visibility.ts';

describe('getVisibleTabNames', () => {
  it('keeps the five website-mirroring tabs whether signed in or out', () => {
    const expected = ['HomeTab', 'NewsTab', 'PhotosTab', 'ArchiveTab', 'ForumTab'];
    assert.deepEqual([...publicTabNames], expected);
    assert.deepEqual([...getVisibleTabNames(false)], expected);
    assert.deepEqual([...getVisibleTabNames(true)], expected);
    assert.equal(signedInOnlyTabNames.length, 0);
  });
});

describe('member-only screens', () => {
  it('gates private messages, compose, and account settings', () => {
    assert.equal(isMemberOnlyScreen('Inbox'), true);
    assert.equal(isMemberOnlyScreen('Conversation'), true);
    assert.equal(isMemberOnlyScreen('Composer'), true);
    assert.equal(isMemberOnlyScreen('Settings'), true);
    assert.equal(isMemberOnlyScreen('DeleteAccount'), true);
    assert.equal(isMemberOnlyScreen('PhotoSubmit'), true);
    assert.equal(isMemberOnlyScreen('SavedList'), true);
  });

  it('keeps archive, photos, forum browse, profile, and contact public', () => {
    assert.equal(isMemberOnlyScreen('Home'), false);
    assert.equal(isMemberOnlyScreen('ArchiveHub'), false);
    assert.equal(isMemberOnlyScreen('NewsIndex'), false);
    assert.equal(isMemberOnlyScreen('PhotoIndex'), false);
    assert.equal(isMemberOnlyScreen('PhotoCategory'), false);
    assert.equal(isMemberOnlyScreen('ForumIndex'), false);
    assert.equal(isMemberOnlyScreen('Category'), false);
    assert.equal(isMemberOnlyScreen('Thread'), false);
    assert.equal(isMemberOnlyScreen('FanPerformances'), false);
    assert.equal(isMemberOnlyScreen('FanPerformanceDetail'), false);
    assert.equal(isMemberOnlyScreen('Contact'), false);
    assert.equal(isMemberOnlyScreen('Profile'), false);
  });
});

describe('shouldHideTabBar', () => {
  it('hides the tab bar on pushed detail routes', () => {
    assert.equal(shouldHideTabBar('Story'), true);
    assert.equal(shouldHideTabBar('BiographyChapter'), true);
    assert.equal(shouldHideTabBar('Album'), true);
    assert.equal(shouldHideTabBar('Thread'), true);
    assert.equal(shouldHideTabBar('PhotoViewer'), true);
    assert.equal(shouldHideTabBar('Profile'), true);
    assert.equal(shouldHideTabBar('DeleteAccount'), true);
    assert.equal(shouldHideTabBar('Contact'), true);
    assert.equal(shouldHideTabBar('Search'), true);
    assert.equal(shouldHideTabBar('Home'), false);
    assert.equal(shouldHideTabBar('NewsIndex'), false);
    assert.equal(shouldHideTabBar('ArchiveHub'), false);
    assert.equal(shouldHideTabBar('PhotoIndex'), false);
    assert.equal(shouldHideTabBar('PhotoCategory'), false);
    assert.equal(shouldHideTabBar('ForumIndex'), false);
    assert.equal(shouldHideTabBar('Biography'), true);
    assert.equal(shouldHideTabBar('Category'), false);
  });
});

describe('archive hub destinations', () => {
  it('lists the eight approved archive rows', () => {
    assert.deepEqual([...ARCHIVE_HUB_IDS], [
      'stories',
      'timeline',
      'biography',
      'discography',
      'tribute',
      'fan-performances',
      'recently-restored',
      'about',
    ]);
    assert.equal(ARCHIVE_HUB_IDS.length, 8);
  });
});
