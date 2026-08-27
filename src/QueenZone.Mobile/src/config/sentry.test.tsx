import * as Sentry from '@sentry/react-native';

jest.mock('expo-constants', () => {
  const extra: Record<string, unknown> = {};
  return {
    __esModule: true,
    default: {
      expoConfig: { extra, version: '0.1.0' },
      manifest2: { extra },
    },
  };
});

function expoExtra(): Record<string, unknown> {
  return require('expo-constants').default.expoConfig.extra as Record<string, unknown>;
}

// The DSN env var is bound via Expo's `expo/virtual/env` re-export of
// process.env at module-import time, so each DSN variant needs a fresh
// module instance (jest.isolateModules) loaded after process.env is set —
// setting process.env after `./sentry` is imported has no effect.
function loadSentryModule(): typeof import('./sentry') {
  let mod!: typeof import('./sentry');
  jest.isolateModules(() => {
    mod = require('./sentry');
  });
  return mod;
}

describe('initSentry', () => {
  const originalEnv = { ...process.env };

  afterEach(() => {
    process.env = { ...originalEnv };
    const extra = expoExtra();
    for (const key of Object.keys(extra)) {
      delete extra[key];
    }
    jest.clearAllMocks();
  });

  it('stays a no-op when no DSN is configured', () => {
    delete process.env.EXPO_PUBLIC_SENTRY_DSN;

    const { initSentry } = loadSentryModule();
    initSentry();

    expect(Sentry.init).not.toHaveBeenCalled();
  });

  it('initializes with tracing enabled once a DSN is configured', () => {
    process.env.EXPO_PUBLIC_SENTRY_DSN = 'https://example@o0.ingest.sentry.io/1';
    delete process.env.EXPO_PUBLIC_SENTRY_TRACES_SAMPLE_RATE;

    const { initSentry, navigationIntegration } = loadSentryModule();
    initSentry();

    expect(Sentry.init).toHaveBeenCalledWith(
      expect.objectContaining({
        dsn: 'https://example@o0.ingest.sentry.io/1',
        tracesSampleRate: 1.0,
        integrations: [navigationIntegration],
      }),
    );
  });

  it('initializes from Expo extra when Metro did not inline the DSN', () => {
    delete process.env.EXPO_PUBLIC_SENTRY_DSN;
    expoExtra().sentryDsn = 'https://extra@o0.ingest.sentry.io/2';
    expoExtra().appEnv = 'staging';

    const { initSentry } = loadSentryModule();
    initSentry();

    expect(Sentry.init).toHaveBeenCalledWith(
      expect.objectContaining({
        dsn: 'https://extra@o0.ingest.sentry.io/2',
        environment: 'staging',
      }),
    );
  });

  it('prefers the baked Expo extra DSN over a stale Metro env value', () => {
    process.env.EXPO_PUBLIC_SENTRY_DSN = 'https://env@o0.ingest.sentry.io/3';
    expoExtra().sentryDsn = 'https://extra@o0.ingest.sentry.io/2';

    const { initSentry } = loadSentryModule();
    initSentry();

    expect(Sentry.init).toHaveBeenCalledWith(
      expect.objectContaining({ dsn: 'https://extra@o0.ingest.sentry.io/2' }),
    );
  });

  it('treats whitespace-only DSN values as unset', () => {
    process.env.EXPO_PUBLIC_SENTRY_DSN = '   ';
    expoExtra().sentryDsn = '  ';

    const { initSentry } = loadSentryModule();
    initSentry();

    expect(Sentry.init).not.toHaveBeenCalled();
  });

  it('honors a custom traces sample rate', () => {
    process.env.EXPO_PUBLIC_SENTRY_DSN = 'https://example@o0.ingest.sentry.io/1';
    process.env.EXPO_PUBLIC_SENTRY_TRACES_SAMPLE_RATE = '0.2';

    const { initSentry } = loadSentryModule();
    initSentry();

    expect(Sentry.init).toHaveBeenCalledWith(
      expect.objectContaining({ tracesSampleRate: 0.2 }),
    );
  });

  it('falls back to 1.0 when the sample rate override is not a number', () => {
    process.env.EXPO_PUBLIC_SENTRY_DSN = 'https://example@o0.ingest.sentry.io/1';
    process.env.EXPO_PUBLIC_SENTRY_TRACES_SAMPLE_RATE = 'not-a-number';

    const { initSentry } = loadSentryModule();
    initSentry();

    expect(Sentry.init).toHaveBeenCalledWith(
      expect.objectContaining({ tracesSampleRate: 1.0 }),
    );
  });
});

describe('reportApiFailure', () => {
  it('records kind, status, method, and path only', () => {
    const { reportApiFailure } = loadSentryModule();
    reportApiFailure({
      kind: 'timeout',
      status: 0,
      method: 'GET',
      path: '/content/news',
    });

    expect(Sentry.addBreadcrumb).toHaveBeenCalledWith({
      category: 'api',
      type: 'http',
      level: 'error',
      data: {
        kind: 'timeout',
        status: 0,
        method: 'GET',
        path: '/content/news',
      },
    });
    expect(Sentry.captureException).not.toHaveBeenCalled();
  });

  it('captures the original fetch error on write offline failures', () => {
    const { reportApiFailure } = loadSentryModule();
    const cause = new TypeError('Network request failed');
    reportApiFailure({
      kind: 'offline',
      status: 0,
      method: 'POST',
      path: '/member/photo-submissions',
      cause,
    });

    expect(Sentry.addBreadcrumb).toHaveBeenCalledWith(
      expect.objectContaining({
        data: expect.objectContaining({ cause: 'Network request failed' }),
      }),
    );
    expect(Sentry.captureException).toHaveBeenCalledWith(
      cause,
      expect.objectContaining({
        extra: expect.objectContaining({
          kind: 'offline',
          path: '/member/photo-submissions',
        }),
      }),
    );
  });

  it('marks client HTTP errors as warnings', () => {
    const { reportApiFailure } = loadSentryModule();
    reportApiFailure({
      kind: 'http',
      status: 404,
      method: 'GET',
      path: '/content/news/1',
    });

    expect(Sentry.addBreadcrumb).toHaveBeenCalledWith(
      expect.objectContaining({ level: 'warning' }),
    );
  });
});
