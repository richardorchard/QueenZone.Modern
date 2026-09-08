import type { AppEnvironment } from '../config/environments';

export type EnvBannerLabel = 'LOCAL' | 'DEV';

/**
 * Mobile non-prod banner copy (#1407). Hide only when `appEnv` is production.
 * Smoke embeds (`QUEENZONE_MOBILE_SMOKE_EMBED`) do not suppress the bar.
 */
export function resolveEnvBannerLabel(appEnv: AppEnvironment): EnvBannerLabel | null {
  if (appEnv === 'production') {
    return null;
  }

  if (appEnv === 'staging') {
    return 'DEV';
  }

  return 'LOCAL';
}
