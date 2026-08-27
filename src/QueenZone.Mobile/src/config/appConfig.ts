/**
 * Runtime app configuration from Expo Constants (#793).
 * Values are baked into `extra` by `app.config.ts` at bundle/start time.
 */
import Constants from 'expo-constants';
import { Platform } from 'react-native';
import {
  resolveApiBaseUrl,
  resolveAppEnvironment,
  rewriteLoopbackForAndroid,
  type AppEnvironment,
} from './environments';

export type AppConfig = {
  appEnv: AppEnvironment;
  /** Origin for `/api/v1` calls (platform-adjusted on Android emulators). */
  apiBaseUrl: string;
  version: string;
  buildNumber?: string;
  buildTimestampUtc?: string;
  buildRevision?: string;
  /** Public Sentry DSN baked at prebuild; unset keeps `initSentry()` a no-op. */
  sentryDsn?: string;
};

type ExpoExtra = {
  appEnv?: string;
  apiBaseUrl?: string;
  buildNumber?: string;
  buildTimestampUtc?: string;
  buildRevision?: string;
  sentryDsn?: string;
};

function readExtra(): ExpoExtra {
  const extra = (Constants.expoConfig?.extra ?? Constants.manifest2?.extra ?? {}) as ExpoExtra;
  return extra;
}

function readSentryDsn(extra: ExpoExtra): string | undefined {
  const fromExtra = typeof extra.sentryDsn === 'string' ? extra.sentryDsn.trim() : '';
  const fromEnv = (process.env.EXPO_PUBLIC_SENTRY_DSN ?? '').trim();
  return fromExtra || fromEnv || undefined;
}

/**
 * Resolved environment + API origin for the running build.
 * Prefer this over reading process.env in UI code — Metro inlines EXPO_PUBLIC_*
 * at bundle time, but `extra` is the single committed contract from app.config.
 */
export function getAppConfig(): AppConfig {
  const extra = readExtra();
  const appEnv = resolveAppEnvironment(
    extra.appEnv ?? process.env.EXPO_PUBLIC_APP_ENV ?? process.env.APP_ENV,
  );
  const configured = resolveApiBaseUrl({
    appEnv,
    override: extra.apiBaseUrl ?? process.env.EXPO_PUBLIC_API_BASE_URL,
  });

  return {
    appEnv,
    apiBaseUrl: rewriteLoopbackForAndroid(configured, Platform.OS),
    version: Constants.expoConfig?.version ?? '0.1.0',
    buildNumber: extra.buildNumber,
    buildTimestampUtc: extra.buildTimestampUtc,
    buildRevision: extra.buildRevision,
    sentryDsn: readSentryDsn(extra),
  };
}

/** Convenience: absolute URL under `/api/v1`. */
export function apiV1Url(path: string): string {
  const { apiBaseUrl } = getAppConfig();
  const suffix = path.startsWith('/') ? path : `/${path}`;
  return `${apiBaseUrl}/api/v1${suffix === '/' ? '' : suffix}`;
}
