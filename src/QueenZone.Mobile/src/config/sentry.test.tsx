import * as Sentry from '@sentry/react-native';

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
