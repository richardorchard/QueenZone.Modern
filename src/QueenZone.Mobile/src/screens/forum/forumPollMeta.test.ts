import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { ApiError } from '../../api/errors.ts';
import {
  formatPollClosesAt,
  formatPollPercentage,
  formatPollResultMeta,
  formatPollStatus,
  pollActionErrorMessage,
  canCastPollVote,
  pollAuthPrompt,
  pollTokenRequiredMessage,
  shouldLoadPoll,
  shouldShowPollLoadError,
  shouldShowPollResults,
} from './forumPollMeta.ts';

describe('forum poll meta', () => {
  it('loads a poll unless the topic header says there is none', () => {
    assert.equal(shouldLoadPoll(true), true);
    assert.equal(shouldLoadPoll(null), true);
    assert.equal(shouldLoadPoll(undefined), true);
    assert.equal(shouldLoadPoll(false), false);
  });

  it('shows website results whenever the GET viewer cannot vote', () => {
    assert.equal(shouldShowPollResults({ canViewerVote: true }), false);
    assert.equal(shouldShowPollResults({ canViewerVote: false }), true);
  });

  it('only allows a vote when the GET flag and a Bearer token are both present', () => {
    assert.equal(canCastPollVote({ canViewerVote: true, hasAccessToken: true }), true);
    assert.equal(canCastPollVote({ canViewerVote: true, hasAccessToken: false }), false);
    assert.equal(canCastPollVote({ canViewerVote: false, hasAccessToken: true }), false);
  });

  it('does not present the development toggle as able to vote', () => {
    const openPoll = { canViewerVote: false, viewerHasVoted: false, isClosed: false };
    assert.equal(
      pollAuthPrompt({ ...openPoll, isSignedIn: false, hasAccessToken: false }),
      'signIn',
    );
    assert.equal(
      pollAuthPrompt({ ...openPoll, isSignedIn: true, hasAccessToken: false }),
      'needsToken',
    );
    assert.equal(
      pollAuthPrompt({
        canViewerVote: true,
        viewerHasVoted: false,
        isClosed: false,
        isSignedIn: true,
        hasAccessToken: true,
      }),
      'none',
    );
    assert.equal(
      pollAuthPrompt({
        canViewerVote: false,
        viewerHasVoted: true,
        isClosed: false,
        isSignedIn: false,
        hasAccessToken: false,
      }),
      'none',
    );
    assert.equal(
      pollAuthPrompt({
        canViewerVote: false,
        viewerHasVoted: false,
        isClosed: true,
        isSignedIn: false,
        hasAccessToken: false,
      }),
      'none',
    );
    assert.match(pollTokenRequiredMessage, /Bearer token/);
    assert.match(pollTokenRequiredMessage, /development sign-in toggle cannot vote/);
  });

  it('formats percentages like website 0.#', () => {
    assert.equal(formatPollPercentage(0), '0%');
    assert.equal(formatPollPercentage(50), '50%');
    assert.equal(formatPollPercentage(66.7), '66.7%');
    assert.equal(formatPollPercentage(Number.NaN), '0%');
    assert.equal(formatPollResultMeta(2, 66.7), '2 · 66.7%');
  });

  it('formats close time and status like the website meta line', () => {
    assert.equal(formatPollClosesAt('2026-08-22T06:43:00Z'), '22 Aug 2026 06:43 UTC');
    assert.equal(formatPollClosesAt('not-a-date'), null);
    assert.equal(
      formatPollStatus({
        isClosed: false,
        closesAt: null,
        distinctVoters: 0,
        isMultiChoice: false,
        maxChoices: null,
      }),
      'Open · 0 voters',
    );
    assert.equal(
      formatPollStatus({
        isClosed: true,
        closesAt: null,
        distinctVoters: 1,
        isMultiChoice: true,
        maxChoices: 2,
      }),
      'Closed · 1 voter · Multi-choice (max 2)',
    );
    assert.equal(
      formatPollStatus({
        isClosed: false,
        closesAt: '2026-08-22T06:43:00Z',
        distinctVoters: 3,
        isMultiChoice: true,
        maxChoices: null,
      }),
      'Closes 22 Aug 2026 06:43 UTC · 3 voters · Multi-choice',
    );
  });

  it('shows a poll load error when GET failed and no poll card can mount', () => {
    assert.equal(shouldShowPollLoadError(null, 'Poll request failed.'), true);
    assert.equal(shouldShowPollLoadError(undefined, 'Poll request failed.'), true);
    assert.equal(shouldShowPollLoadError(null, null), false);
    assert.equal(shouldShowPollLoadError({ pollId: '1' }, 'You have already voted.'), false);
  });

  it('surfaces API vote errors', () => {
    assert.equal(
      pollActionErrorMessage(
        new ApiError(409, 'You have already voted in this poll. Votes cannot be changed.'),
      ),
      'You have already voted in this poll. Votes cannot be changed.',
    );
    assert.equal(pollActionErrorMessage('nope'), 'Something went wrong.');
  });
});
