import type { ConfigContext, ExpoConfig } from 'expo/config';

// CommonJS shared module — Expo loads app.config via require(), not Metro.
// eslint-disable-next-line @typescript-eslint/no-require-imports
const {
  marketingVersionPrefix,
  resolveApiBaseUrl,
  resolveAppEnvironment,
  resolveIosBuildNumber,
  resolveMarketingVersion,
} = require('./apiEnvironments.cjs') as typeof import('./apiEnvironments.cjs');

/**
 * Dynamic Expo config: embeds appEnv + apiBaseUrl into `extra` so native and
 * JS builds can switch backends without code changes (#793).
 *
 * Override at start/build time:
 *   EXPO_PUBLIC_APP_ENV=staging|production|development
 *   EXPO_PUBLIC_API_BASE_URL=https://localhost:7162
 *   IOS_BUILD_NUMBER=<positive integer> (TestFlight CFBundleVersion; see GITHUB_RUN_NUMBER)
 *   GITHUB_RUN_NUMBER=<positive integer> (store Version `{prefix}.{run}` + integer build)
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
  const version = resolveMarketingVersion({
    prefix: marketingVersionPrefix,
    runNumber: process.env.GITHUB_RUN_NUMBER,
  });
  return {
    ...config,
    name: config.name ?? 'QueenZone',
    slug: config.slug ?? 'queenzone-mobile',
    version,
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
      // Baked at prebuild into EXConstants so published JS bundles still
      // initialize Sentry when Metro does not inherit EXPO_PUBLIC_SENTRY_DSN.
      sentryDsn: (process.env.EXPO_PUBLIC_SENTRY_DSN ?? '').trim() || undefined,
    },
    plugins: [
      ...(config.plugins ?? []),
      // CNG: android/ is generated in CI. Force WorkManager 2.8.1 so
      // react-native-android-widget's work-runtime does not clash with a
      // transitive work-runtime-ktx 2.7.1 (duplicate OneTimeWorkRequestKt).
      './plugins/withAndroidWorkRuntimeAlignment.cjs',
      // After expo-widgets writes ExpoWidgetsTarget: render On This Day in
      // SwiftUI so the gallery/home snapshot is never a Release EmptyView.
      './plugins/withIosOnThisDayNativeWidget.cjs',
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
        // ADR 0014: direct APNs/FCM, no EAS. Expo SDK 57 always generates
        // aps-environment=development at prebuild; Xcode changes it to
        // production when archiving with the App Store distribution profile.
        'expo-notifications',
        {
          icon: './assets/ic-notification.png',
          color: '#B89A4A',
          defaultChannel: 'default',
        },
      ],
    ],
  };
};
