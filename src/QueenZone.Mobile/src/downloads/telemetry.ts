import * as Sentry from '@sentry/react-native';

/** Dev log + Sentry breadcrumb for download finalize (QUEENZONE-MOBILE-A). */
export function reportDownloadBreadcrumb(
  message: string,
  data: Record<string, string | number | boolean | null | undefined>,
): void {
  if (typeof __DEV__ !== 'undefined' && __DEV__ && !process.env.JEST_WORKER_ID) {
    console.info(`[downloads] ${message}`, data);
  }
  Sentry.addBreadcrumb({
    category: 'download',
    level: 'info',
    message,
    data,
  });
}
