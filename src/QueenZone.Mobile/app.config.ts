import type { ConfigContext, ExpoConfig } from 'expo/config';

// CommonJS shared module — Expo loads app.config via require(), not Metro.
// eslint-disable-next-line @typescript-eslint/no-require-imports
const {
  resolveApiBaseUrl,
  resolveAppEnvironment,
} = require('./apiEnvironments.cjs') as typeof import('./apiEnvironments.cjs');

/**
 * Dynamic Expo config: embeds appEnv + apiBaseUrl into `extra` so native and
 * JS builds can switch backends without code changes (#793).
 *
 * Override at start/build time:
 *   EXPO_PUBLIC_APP_ENV=staging|production|development
 *   EXPO_PUBLIC_API_BASE_URL=https://localhost:7162
 */
export default ({ config }: ConfigContext): ExpoConfig => {
  const appEnv = resolveAppEnvironment(
    process.env.EXPO_PUBLIC_APP_ENV ?? process.env.APP_ENV,
  );
  const apiBaseUrl = resolveApiBaseUrl({
    appEnv,
    override: process.env.EXPO_PUBLIC_API_BASE_URL,
  });

  return {
    ...config,
    name: config.name ?? 'QueenZone',
    slug: config.slug ?? 'queenzone-mobile',
    extra: {
      ...(typeof config.extra === 'object' && config.extra !== null ? config.extra : {}),
      appEnv,
      apiBaseUrl,
    },
  };
};
