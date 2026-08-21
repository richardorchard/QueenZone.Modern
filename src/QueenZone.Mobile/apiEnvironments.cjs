/**
 * Shared API environment defaults for app.config (CommonJS) and the RN app.
 * Keep types/tests in src/config/environments.ts wrapping this module.
 * @type {const}
 */
const defaultApiBaseUrls = {
  development: 'http://localhost:5146',
  staging: 'https://queenzone-dev.azurewebsites.net',
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

module.exports = {
  defaultApiBaseUrls,
  normalizeApiBaseUrl,
  resolveApiBaseUrl,
  resolveAppEnvironment,
  rewriteLoopbackForAndroid,
};
