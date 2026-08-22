import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  avatarUrl,
  formatMemberSince,
  parseDeletionRequested,
  parseMemberProfile,
  validateDisplayName,
} from './me.ts';

const sampleProfile = {
  memberId: '11111111-1111-1111-1111-111111111111',
  email: 'fan@example.com',
  displayName: 'Roger O',
  createdAt: '2004-06-01T00:00:00Z',
  lastLoginAt: null,
  hasAvatar: true,
  avatarPath: '/account/avatar/11111111-1111-1111-1111-111111111111',
  avatarThumbPath: '/account/avatar/11111111-1111-1111-1111-111111111111?size=thumb',
  messagePrivacy: 'followed',
  linkedProviders: ['Google'],
  legacyLink: {
    kind: 'linked',
    match: { userId: 42, username: 'ClassicFan' },
    claimableMatches: [],
    unavailableMatches: [],
  },
  scheduledDeletionAt: null,
  limits: {
    minDisplayNameLength: 2,
    maxDisplayNameLength: 100,
    maxAvatarBytes: 2097152,
    allowedAvatarContentTypes: ['image/jpeg', 'image/png', 'image/webp'],
    deletionRetentionDays: 30,
  },
  deletion: {
    confirmationPhrase: 'DELETE',
    confirmationHint: 'Type DELETE to schedule deletion of the account.',
    requestedTitle: 'Account deletion scheduled',
    requestedMessage: 'You have been signed out.',
    whatHappens: ['You are signed out after requesting deletion.'],
  },
};

describe('parseMemberProfile', () => {
  it('reads the account settings contract', () => {
    const profile = parseMemberProfile(sampleProfile);
    assert.equal(profile.displayName, 'Roger O');
    assert.equal(profile.messagePrivacy, 'followed');
    assert.equal(profile.legacyLink.kind, 'linked');
    assert.equal(profile.legacyLink.match?.userId, 42);
    assert.equal(profile.limits.minDisplayNameLength, 2);
    assert.equal(profile.deletion.confirmationPhrase, 'DELETE');
  });
});

describe('validateDisplayName', () => {
  it('matches website length rules', () => {
    assert.equal(validateDisplayName('A'), 'Display name must be at least 2 characters.');
    assert.equal(validateDisplayName('  '), 'Display name is required.');
    assert.equal(validateDisplayName('Roger'), null);
  });
});

describe('avatarUrl', () => {
  it('joins the website serve path onto the API origin', () => {
    assert.equal(
      avatarUrl('http://localhost:5146', '/account/avatar/abc'),
      'http://localhost:5146/account/avatar/abc',
    );
    assert.equal(
      avatarUrl('http://localhost:5146/', '/account/avatar/abc', '1'),
      'http://localhost:5146/account/avatar/abc?v=1',
    );
    assert.equal(avatarUrl('http://localhost:5146', null), null);
  });
});

describe('formatMemberSince', () => {
  it('formats month and year', () => {
    assert.match(formatMemberSince('2004-06-01T00:00:00Z'), /June 2004/);
  });
});

describe('parseDeletionRequested', () => {
  it('requires requested true', () => {
    const result = parseDeletionRequested({
      requested: true,
      scheduledDeletionAt: '2026-09-21T00:00:00Z',
      title: 'Account deletion scheduled',
      message: 'You have been signed out.',
    });
    assert.equal(result.title, 'Account deletion scheduled');
  });
});
