import AsyncStorage from '@react-native-async-storage/async-storage';
import { createTelemetryDeck } from '@typedigital/telemetrydeck-react';
import * as Crypto from 'expo-crypto';
import { Platform } from 'react-native';
import { TextEncoder as TextEncoderPolyfill } from 'text-encoding';
import { getAppConfig, type AppConfig } from '../config/appConfig';
import {
  regionFromLocale,
  sectionFromNavigationState,
  type AnalyticsSection,
} from './catalog';
import { isAnalyticsConsentGranted, persistAnalyticsConsent } from './consent';

const installationIdKey = 'queenzone.mobile.analyticsInstallationId';
const lastActiveDayKey = 'queenzone.mobile.analyticsLastActiveDay';

type NavigationStateLike = Parameters<typeof sectionFromNavigationState>[0];
type TelemetryDeckClient = ReturnType<typeof createTelemetryDeck>;

let clientPromise: Promise<TelemetryDeckClient | null> | null = null;
let lastSection: AnalyticsSection | null = null;
let dailyActiveQueue: Promise<void> = Promise.resolve();
let configOverride: AppConfig | null = null;

function appConfig(): AppConfig {
  return configOverride ?? getAppConfig();
}

function ensureTextEncoder(): void {
  if (typeof globalThis.TextEncoder === 'undefined') {
    Object.defineProperty(globalThis, 'TextEncoder', {
      configurable: true,
      value: TextEncoderPolyfill,
      writable: true,
    });
  }
}

function deviceLocale(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().locale || 'und';
  } catch {
    return 'und';
  }
}

async function getOrCreateInstallationId(): Promise<string> {
  const existing = await AsyncStorage.getItem(installationIdKey);
  if (existing) {
    return existing;
  }

  const created = Crypto.randomUUID();
  await AsyncStorage.setItem(installationIdKey, created);
  return created;
}

async function createClient(): Promise<TelemetryDeckClient | null> {
  const config = appConfig();
  if (!config.telemetryDeckAppId || !(await isAnalyticsConsentGranted())) {
    return null;
  }

  ensureTextEncoder();
  const installationId = await getOrCreateInstallationId();
  return createTelemetryDeck({
    appID: config.telemetryDeckAppId,
    clientUser: installationId,
    testMode: config.appEnv !== 'production',
    // Inject only the digest implementation. Do not replace global crypto;
    // OAuth and push registration also depend on expo-crypto.
    subtleCrypto: { digest: Crypto.digest } as unknown as Function,
  });
}

function getClient(): Promise<TelemetryDeckClient | null> {
  clientPromise ??= createClient().catch((error: unknown) => {
    if (__DEV__) {
      console.warn('TelemetryDeck initialization failed; analytics is disabled.', error);
    }
    return null;
  });
  return clientPromise;
}

function basePayload(): Record<string, string> {
  const config = appConfig();
  const locale = deviceLocale();
  const region = regionFromLocale(locale);

  return {
    'TelemetryDeck.AppInfo.version': config.version,
    ...(config.buildNumber
      ? { 'TelemetryDeck.AppInfo.buildNumber': config.buildNumber }
      : {}),
    'TelemetryDeck.Device.platform': Platform.OS,
    'TelemetryDeck.RunContext.locale': locale,
    'TelemetryDeck.RunContext.targetEnvironment': 'native',
    ...(region ? { 'TelemetryDeck.UserPreference.region': region } : {}),
  };
}

async function signal(type: 'app.active' | 'section.viewed', payload = {}): Promise<void> {
  if (!(await isAnalyticsConsentGranted())) {
    return;
  }
  const client = await getClient();
  if (!client) {
    return;
  }

  try {
    await client.signal(type, { ...basePayload(), ...payload });
  } catch {
    // Product analytics is best-effort and must never affect the user journey
    // or recursively report its own failure through Sentry.
  }
}

async function trackDailyActiveInternal(now: Date): Promise<void> {
  const client = await getClient();
  if (!client) {
    return;
  }

  const day = now.toISOString().slice(0, 10);
  try {
    if ((await AsyncStorage.getItem(lastActiveDayKey)) === day) {
      return;
    }
    // Mark before sending: an outage must not create a retry storm or consume
    // the free event allowance when connectivity returns.
    await AsyncStorage.setItem(lastActiveDayKey, day);
  } catch {
    return;
  }

  await signal('app.active');
}

/** At most one active-installation event per UTC day, persisted across launches. */
export function trackDailyActive(now = new Date()): Promise<void> {
  dailyActiveQueue = dailyActiveQueue.then(() => trackDailyActiveInternal(now));
  return dailyActiveQueue;
}

export async function trackSectionViewed(section: AnalyticsSection | null): Promise<void> {
  if (!section || section === lastSection || !(await isAnalyticsConsentGranted())) {
    return;
  }
  lastSection = section;
  await signal('section.viewed', { section });
}

export function trackNavigationState(state: NavigationStateLike): Promise<void> {
  return trackSectionViewed(sectionFromNavigationState(state));
}

/** Persist consent, stop future delivery immediately, and forget local analytics identity on withdrawal. */
export async function updateAnalyticsConsent(granted: boolean): Promise<void> {
  await persistAnalyticsConsent(granted);
  clientPromise = null;
  lastSection = null;
  dailyActiveQueue = Promise.resolve();

  if (!granted) {
    await AsyncStorage.multiRemove([installationIdKey, lastActiveDayKey]);
  }
}

/** Test isolation only. */
export function resetAnalyticsForTests(config: AppConfig | null = null): void {
  clientPromise = null;
  lastSection = null;
  dailyActiveQueue = Promise.resolve();
  configOverride = config;
}
