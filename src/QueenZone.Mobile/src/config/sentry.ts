/**
 * Crash/error monitoring bootstrap (#855).
 *
 * DSN is public-by-design (Sentry DSNs are not secrets) and comes from
 * EXPO_PUBLIC_SENTRY_DSN so Metro inlines it at bundle time like the other
 * EXPO_PUBLIC_* config in `environments.ts`. Sentry stays disabled — init()
 * is a no-op — until a DSN is configured, so builds work before Sentry is
 * set up.
 */
import * as Sentry from '@sentry/react-native';
import { resolveAppEnvironment } from './environments';

export function initSentry(): void {
  const dsn = process.env.EXPO_PUBLIC_SENTRY_DSN;
  if (!dsn) {
    return;
  }

  Sentry.init({
    dsn,
    environment: resolveAppEnvironment(
      process.env.EXPO_PUBLIC_APP_ENV ?? process.env.APP_ENV,
    ),
    enableAutoSessionTracking: true,
  });
}
