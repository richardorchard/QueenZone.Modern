/**
 * Crash/error monitoring bootstrap (#855, #886).
 *
 * DSN is public-by-design (Sentry DSNs are not secrets) and comes from
 * EXPO_PUBLIC_SENTRY_DSN so Metro inlines it at bundle time like the other
 * EXPO_PUBLIC_* config in `environments.ts`. Sentry stays disabled — init()
 * is a no-op — until a DSN is configured, so builds work before Sentry is
 * set up.
 *
 * Performance tracing needs its own opt-in on top of the DSN: without a
 * tracesSampleRate and the react-native tracing integration, Sentry only
 * ever reports errors/crashes — the Performance/Traces views stay empty
 * even with a valid DSN. `navigationIntegration` is exported so App.tsx can
 * register the NavigationContainer ref and get route-change spans.
 */
import * as Sentry from '@sentry/react-native';
import { resolveAppEnvironment } from './environments';

export const navigationIntegration = Sentry.reactNavigationIntegration();

export function reportApiFailure(event: {
  kind: string;
  status: number;
  method: string;
  path: string;
  cause?: unknown;
}): void {
  const causeMessage = event.cause instanceof Error ? event.cause.message : undefined;
  Sentry.addBreadcrumb({
    category: 'api',
    type: 'http',
    level: event.kind === 'http' && event.status < 500 ? 'warning' : 'error',
    data: {
      kind: event.kind,
      status: event.status,
      method: event.method,
      path: event.path,
      ...(causeMessage ? { cause: causeMessage } : {}),
    },
  });

  if (event.method === 'GET') {
    return;
  }

  if (event.kind !== 'offline' && event.kind !== 'timeout' && event.kind !== 'local-file') {
    return;
  }

  Sentry.captureException(event.cause instanceof Error ? event.cause : new Error(`api ${event.kind}`), {
    extra: {
      kind: event.kind,
      status: event.status,
      method: event.method,
      path: event.path,
    },
  });
}

export function initSentry(): void {
  const dsn = process.env.EXPO_PUBLIC_SENTRY_DSN;
  if (!dsn) {
    return;
  }

  const tracesSampleRate = Number(
    process.env.EXPO_PUBLIC_SENTRY_TRACES_SAMPLE_RATE ?? '1.0',
  );

  Sentry.init({
    dsn,
    environment: resolveAppEnvironment(
      process.env.EXPO_PUBLIC_APP_ENV ?? process.env.APP_ENV,
    ),
    enableAutoSessionTracking: true,
    tracesSampleRate: Number.isFinite(tracesSampleRate) ? tracesSampleRate : 1.0,
    integrations: [navigationIntegration],
  });
}
