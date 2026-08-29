/**
 * Per-environment API base URL defaults (#793).
 *
 * Implementation lives in `apiEnvironments.cjs` so Expo's `app.config.ts`
 * (CommonJS load) and the RN app share one source of truth.
 *
 * Aligns with ASP.NET environments: Development / Staging / Production.
 * Local QueenZone.Web defaults to http://localhost:5146 (launchSettings "http").
 */
// eslint-disable-next-line @typescript-eslint/no-require-imports
const impl = require('../../apiEnvironments.cjs') as {
  defaultApiBaseUrls: Record<AppEnvironment, string>;
  marketingVersionPrefix: string;
  resolveAppEnvironment: (raw: string | undefined | null) => AppEnvironment;
  resolveApiBaseUrl: (input: ResolveApiBaseUrlInput) => string;
  normalizeApiBaseUrl: (raw: string) => string;
  resolveIosBuildNumber: (input?: ResolveIosBuildNumberInput) => string;
  resolveMarketingVersion: (input?: ResolveMarketingVersionInput) => string;
  rewriteLoopbackForAndroid: (apiBaseUrl: string, platform: string) => string;
};

export type AppEnvironment = 'development' | 'staging' | 'production';

export type ResolveApiBaseUrlInput = {
  appEnv: AppEnvironment;
  override?: string | undefined | null;
};

export type ResolveIosBuildNumberInput = {
  override?: string | undefined | null;
  githubRunNumber?: string | undefined | null;
  fallback?: string | undefined | null;
};

export type ResolveMarketingVersionInput = {
  prefix?: string | undefined | null;
  runNumber?: string | number | undefined | null;
};

export const defaultApiBaseUrls = impl.defaultApiBaseUrls;
export const marketingVersionPrefix = impl.marketingVersionPrefix;
export const resolveAppEnvironment = impl.resolveAppEnvironment;
export const resolveApiBaseUrl = impl.resolveApiBaseUrl;
export const normalizeApiBaseUrl = impl.normalizeApiBaseUrl;
export const resolveIosBuildNumber = impl.resolveIosBuildNumber;
export const resolveMarketingVersion = impl.resolveMarketingVersion;
export const rewriteLoopbackForAndroid = impl.rewriteLoopbackForAndroid;
