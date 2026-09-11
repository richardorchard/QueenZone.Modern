/**
 * Stale-while-revalidate windows for `withOfflineCache` (issue #1477).
 * Per-key, not global: the home screen's eight sections tolerate staleness
 * very differently, per the issue's guidance.
 */
export const HOME_SLOW_CHANGING_TTL_MS = 5 * 60_000; // on-this-day, random quote
export const HOME_MODERATE_TTL_MS = 60_000; // news, forum threads, photo categories, poll
export const HOME_LIVE_TTL_MS = 20_000; // live activity, inbox preview
