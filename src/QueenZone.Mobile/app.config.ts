import type { ConfigContext, ExpoConfig } from 'expo/config';

// CommonJS shared module — Expo loads app.config via require(), not Metro.
// eslint-disable-next-line @typescript-eslint/no-require-imports
const {
  resolveApiBaseUrl,
  resolveAppEnvironment,
  resolveIosBuildNumber,
} = require('./apiEnvironments.cjs') as typeof import('./apiEnvironments.cjs');

/**
 * Dynamic Expo config: embeds appEnv + apiBaseUrl into `extra` so native and
 * JS builds can switch backends without code changes (#793).
 *
 * Override at start/build time:
 *   EXPO_PUBLIC_APP_ENV=staging|production|development
 *   EXPO_PUBLIC_API_BASE_URL=https://localhost:7162
 *   IOS_BUILD_NUMBER=<positive integer> (TestFlight CFBundleVersion; see GITHUB_RUN_NUMBER)
 */
export default ({ config }: ConfigContext): ExpoConfig => {
  const appEnv = resolveAppEnvironment(
    process.env.EXPO_PUBLIC_APP_ENV ?? process.env.APP_ENV,
  );
  const apiBaseUrl = resolveApiBaseUrl({
    appEnv,
    override: process.env.EXPO_PUBLIC_API_BASE_URL,
  });
  const iosBuildNumber = resolveIosBuildNumber({
    override: process.env.IOS_BUILD_NUMBER,
    githubRunNumber: process.env.GITHUB_RUN_NUMBER,
    fallback: config.ios?.buildNumber,
  });

  return {
    ...config,
    name: config.name ?? 'QueenZone',
    slug: config.slug ?? 'queenzone-mobile',
    ios: {
      ...(typeof config.ios === 'object' && config.ios !== null ? config.ios : {}),
      buildNumber: iosBuildNumber,
      config: {
        ...(typeof config.ios?.config === 'object' && config.ios.config !== null
          ? config.ios.config
          : {}),
        // QueenZone uses only exempt platform HTTPS; declaring this prevents
        // every TestFlight upload from pausing for export-compliance answers.
        usesNonExemptEncryption: false,
      },
    },
    extra: {
      ...(typeof config.extra === 'object' && config.extra !== null ? config.extra : {}),
      appEnv,
      apiBaseUrl,
      buildNumber: iosBuildNumber,
      buildTimestampUtc: process.env.BUILD_TIMESTAMP_UTC,
      buildRevision: process.env.BUILD_REVISION,
    },
  };
};
