/**
 * Shared API environment defaults for app.config (CommonJS) and the RN app.
 * Keep types/tests in src/config/environments.ts wrapping this module.
 * @type {const}
 */
const defaultApiBaseUrls = {
  development: 'http://localhost:5146',
  staging: 'https://www.queenzone.org',
  production: 'https://www.queenzone.org',
};

/**
 * @param {string | undefined | null} raw
 * @returns {'development' | 'staging' | 'production'}
 */
function resolveAppEnvironment(raw) {
  const value = (raw ?? '').trim().toLowerCase();
  if (!value) {
    return 'development';
  }
  if (value === 'prod') {
    return 'production';
  }
  if (value === 'stage') {
    return 'staging';
  }
  if (value === 'dev') {
    return 'development';
  }
  if (value === 'development' || value === 'staging' || value === 'production') {
    return value;
  }
  throw new Error(
    `Unknown app environment "${raw}". Use development, staging, or production (or EXPO_PUBLIC_API_BASE_URL alone).`,
  );
}

/**
 * @param {string} raw
 * @returns {string}
 */
function normalizeApiBaseUrl(raw) {
  const trimmed = raw.trim().replace(/\/+$/, '');
  if (!trimmed) {
    throw new Error('API base URL must not be empty.');
  }

  let url;
  try {
    url = new URL(trimmed);
  } catch {
    throw new Error(`API base URL is not a valid absolute URL: "${raw}"`);
  }

  if (url.protocol !== 'http:' && url.protocol !== 'https:') {
    throw new Error(`API base URL must be http(s): "${raw}"`);
  }

  return url.origin;
}

/**
 * @param {{ appEnv: 'development' | 'staging' | 'production', override?: string | null }} input
 * @returns {string}
 */
function resolveApiBaseUrl({ appEnv, override }) {
  const raw = override?.trim() ? override.trim() : defaultApiBaseUrls[appEnv];
  return normalizeApiBaseUrl(raw);
}

/**
 * @param {string} apiBaseUrl
 * @param {string} platform
 * @returns {string}
 */
function rewriteLoopbackForAndroid(apiBaseUrl, platform) {
  if (platform !== 'android') {
    return apiBaseUrl;
  }

  const url = new URL(apiBaseUrl);
  if (url.hostname === 'localhost' || url.hostname === '127.0.0.1') {
    url.hostname = '10.0.2.2';
  }
  return url.origin;
}

/**
 * iOS CFBundleVersion / Expo `ios.buildNumber`.
 *
 * App Store Connect rejects uploads when CFBundleVersion is not greater than
 * the previously uploaded build. Expo writes a literal build number into
 * Info.plist at prebuild time, so CI must set IOS_BUILD_NUMBER (or
 * GITHUB_RUN_NUMBER) before `expo prebuild` — passing CURRENT_PROJECT_VERSION
 * to xcodebuild alone does not change a hardcoded Info.plist value.
 *
 * @param {{ override?: string | null, githubRunNumber?: string | null, fallback?: string | null }} [input]
 * @returns {string}
 */
function resolveIosBuildNumber(input = {}) {
  const candidates = [input.override, input.githubRunNumber, input.fallback, '1'];
  for (const raw of candidates) {
    const value = (raw ?? '').trim();
    if (!value) {
      continue;
    }
    if (!/^[0-9]+$/.test(value) || Number(value) < 1) {
      throw new Error(
        `iOS build number must be a positive integer (got "${raw}").`,
      );
    }
    // Normalize leading zeros (Apple accepts numeric strings; keep canonical form).
    return String(Number(value));
  }
  return '1';
}

/**
 * iOS `aps-environment` entitlement for the expo-notifications `mode`.
 *
 * App Store and TestFlight distribution profiles only include
 * `aps-environment=production`. Sandbox (`development`) is valid only for
 * locally installed development-signed builds. Staging TestFlight builds
 * still talk to the staging API, but they must use the production
 * entitlement — ADR 0014 and `PushNotifications__Apns__Environment` are
 * production for those uploads.
 *
 * @param {{ override?: string | null, appEnv?: string | null, distributionBuild?: boolean }} [input]
 * @returns {'production' | 'development'}
 */
function resolveIosApsEnvironment(input = {}) {
  const raw = (input.override ?? '').trim().toLowerCase();
  if (raw === 'production' || raw === 'development') {
    return raw;
  }
  if (raw) {
    throw new Error(
      `iOS APS environment must be production or development (got "${input.override}").`,
    );
  }
  if (input.distributionBuild === true || input.appEnv === 'production') {
    return 'production';
  }
  return 'development';
}

module.exports = {
  defaultApiBaseUrls,
  normalizeApiBaseUrl,
  resolveApiBaseUrl,
  resolveAppEnvironment,
  resolveIosApsEnvironment,
  resolveIosBuildNumber,
  rewriteLoopbackForAndroid,
};
