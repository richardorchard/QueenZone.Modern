import { ApiError } from '../../api';
import type { ForumPoll } from '../../api';

const utcMonths = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

/** Same defensive load as the website topic page: null means unknown. */
export function shouldLoadPoll(hasPoll: boolean | null | undefined): boolean {
  return hasPoll !== false;
}

/** Website `_ForumPoll.cshtml` shows results whenever the viewer cannot vote. */
export function shouldShowPollResults(poll: Pick<ForumPoll, 'canViewerVote'>): boolean {
  return !poll.canViewerVote;
}

export function shouldShowSignInToVote(
  poll: Pick<ForumPoll, 'canViewerVote' | 'viewerHasVoted' | 'isClosed'>,
): boolean {
  return !poll.canViewerVote && !poll.viewerHasVoted && !poll.isClosed;
}

/** Matches website `option.Percentage.ToString("0.#")`. */
export function formatPollPercentage(value: number): string {
  if (!Number.isFinite(value)) {
    return '0%';
  }
  const rounded = Math.round(value * 10) / 10;
  return `${Number.isInteger(rounded) ? String(rounded) : rounded.toFixed(1)}%`;
}

/** Matches website `dd MMM yyyy HH:mm UTC`. */
export function formatPollClosesAt(iso: string | null | undefined): string | null {
  if (!iso) {
    return null;
  }
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return null;
  }
  const day = String(date.getUTCDate()).padStart(2, '0');
  const month = utcMonths[date.getUTCMonth()] ?? '';
  const year = date.getUTCFullYear();
  const hours = String(date.getUTCHours()).padStart(2, '0');
  const minutes = String(date.getUTCMinutes()).padStart(2, '0');
  return `${day} ${month} ${year} ${hours}:${minutes} UTC`;
}

export function formatPollStatus(poll: Pick<ForumPoll, 'isClosed' | 'closesAt' | 'distinctVoters' | 'isMultiChoice' | 'maxChoices'>): string {
  const voterLabel =
    poll.distinctVoters === 1 ? '1 voter' : `${poll.distinctVoters.toLocaleString('en-US')} voters`;

  let state: string;
  if (poll.isClosed) {
    state = 'Closed';
  } else if (poll.closesAt) {
    const closes = formatPollClosesAt(poll.closesAt);
    state = closes ? `Closes ${closes}` : 'Open';
  } else {
    state = 'Open';
  }

  const parts = [state, voterLabel];
  if (poll.isMultiChoice) {
    parts.push(poll.maxChoices != null ? `Multi-choice (max ${poll.maxChoices})` : 'Multi-choice');
  }
  return parts.join(' · ');
}

export function formatPollResultMeta(voteCount: number, percentage: number): string {
  return `${voteCount.toLocaleString('en-US')} · ${formatPollPercentage(percentage)}`;
}

export function pollActionErrorMessage(err: unknown): string {
  if (err instanceof ApiError) {
    return err.message;
  }
  return 'Something went wrong.';
}
