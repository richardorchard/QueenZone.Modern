import type { ConfigContext, ExpoConfig } from 'expo/config';

// CommonJS shared module — Expo loads app.config via require(), not Metro.
// eslint-disable-next-line @typescript-eslint/no-require-imports
const {
  resolveApiBaseUrl,
  resolveAppEnvironment,
  resolveIosApsEnvironment,
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
 *   IOS_APS_ENVIRONMENT=production|development (TestFlight must be production)
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
  const iosApsEnvironment = resolveIosApsEnvironment({
    override: process.env.IOS_APS_ENVIRONMENT,
    appEnv,
    distributionBuild:
      process.env.IOS_DISTRIBUTION_BUILD === '1' ||
      process.env.IOS_DISTRIBUTION_BUILD === 'true',
  });

  return {
    ...config,
    name: config.name ?? 'QueenZone',
    slug: config.slug ?? 'queenzone-mobile',
    android: {
      ...(typeof config.android === 'object' && config.android !== null
        ? config.android
        : {}),
      googleServicesFile: './google-services.json',
    },
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
    plugins: [
      ...(config.plugins ?? []),
      // CNG: android/ is generated in CI. Force WorkManager 2.8.1 so
      // react-native-android-widget's work-runtime does not clash with a
      // transitive work-runtime-ktx 2.7.1 (duplicate OneTimeWorkRequestKt).
      './plugins/withAndroidWorkRuntimeAlignment.cjs',
      [
        // Auth token is deliberately NOT passed here — sentry-cli picks up
        // SENTRY_AUTH_TOKEN from the build environment directly (Gradle /
        // Xcode build phase), so it never lands in the generated,
        // gitignored sentry.properties file. See publish-*.yml workflows.
        // CI unsigned builds set SENTRY_DISABLE_AUTO_UPLOAD=true so missing
        // org/token does not fail the Bundle RN script (#857).
        '@sentry/react-native',
        {
          ...(process.env.SENTRY_ORG ? { organization: process.env.SENTRY_ORG } : {}),
          ...(process.env.SENTRY_PROJECT ? { project: process.env.SENTRY_PROJECT } : {}),
        },
      ],
      [
        // ADR 0014: direct APNs/FCM, no EAS. `mode` sets the iOS
        // `aps-environment` entitlement. App Store / TestFlight profiles
        // only include production; sandbox is for development-signed local
        // installs. Staging TestFlight still uses production here.
        'expo-notifications',
        {
          icon: './assets/ic-notification.png',
          color: '#B89A4A',
          defaultChannel: 'default',
          mode: iosApsEnvironment,
        },
      ],
    ],
  };
};
