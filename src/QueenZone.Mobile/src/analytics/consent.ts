import AsyncStorage from '@react-native-async-storage/async-storage';
import { useEffect, useSyncExternalStore } from 'react';

const consentKey = 'queenzone.mobile.analyticsConsent';

export type AnalyticsConsent = 'loading' | 'unset' | 'granted' | 'denied';

let consent: AnalyticsConsent = 'loading';
let loadPromise: Promise<AnalyticsConsent> | null = null;
const listeners = new Set<() => void>();

function publish(next: AnalyticsConsent): AnalyticsConsent {
  consent = next;
  listeners.forEach((listener) => listener());
  return next;
}

function subscribe(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function loadAnalyticsConsent(): Promise<AnalyticsConsent> {
  loadPromise ??= AsyncStorage.getItem(consentKey)
    .then((stored) => publish(stored === 'granted' || stored === 'denied' ? stored : 'unset'))
    .catch(() => publish('unset'));
  return loadPromise;
}

export async function isAnalyticsConsentGranted(): Promise<boolean> {
  return (await loadAnalyticsConsent()) === 'granted';
}

export async function persistAnalyticsConsent(granted: boolean): Promise<void> {
  const next = granted ? 'granted' : 'denied';
  await AsyncStorage.setItem(consentKey, next);
  loadPromise = Promise.resolve(next);
  publish(next);
}

export function useAnalyticsConsent(): AnalyticsConsent {
  const snapshot = useSyncExternalStore(subscribe, () => consent, () => consent);
  useEffect(() => {
    if (snapshot === 'loading') {
      void loadAnalyticsConsent();
    }
  }, [snapshot]);
  return snapshot;
}

/** Test isolation only. */
export function resetAnalyticsConsentForTests(): void {
  consent = 'loading';
  loadPromise = null;
  listeners.clear();
}
