/** Development / Simulator SecureStore can hang instead of rejecting (#1387). */
export const developmentSessionRestoreTimeoutMs = 5_000;
export const sessionRestoreTimeoutLabel = 'session-restore-timeout';

export function withTimeout<T>(promise: Promise<T>, ms: number, label: string): Promise<T> {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      reject(new Error(label));
    }, ms);
    promise.then(
      (value) => {
        clearTimeout(timer);
        resolve(value);
      },
      (error: unknown) => {
        clearTimeout(timer);
        reject(error);
      },
    );
  });
}

export function isSessionRestoreTimeoutError(error: unknown): boolean {
  return error instanceof Error && error.message === sessionRestoreTimeoutLabel;
}
