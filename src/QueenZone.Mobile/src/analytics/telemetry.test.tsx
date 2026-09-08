import AsyncStorage from '@react-native-async-storage/async-storage';
import * as Crypto from 'expo-crypto';
import { createTelemetryDeck } from '@typedigital/telemetrydeck-react';
import {
  resetAnalyticsForTests,
  trackDailyActive,
  trackSectionViewed,
  updateAnalyticsConsent,
} from './telemetry';
import { resetAnalyticsConsentForTests } from './consent';

const mockSignal = jest.fn(async () => new Response());
const productionConfig = {
  appEnv: 'production',
  apiBaseUrl: 'https://www.queenzone.org',
  version: '1.2.3',
  buildNumber: '42',
  telemetryDeckAppId: 'telemetry-app-id',
} as const;

jest.mock('@typedigital/telemetrydeck-react', () => ({
  createTelemetryDeck: jest.fn(() => ({ signal: mockSignal })),
}));

jest.mock('expo-crypto', () => ({
  randomUUID: jest.fn(() => '67c239a7-77be-4e3b-bac4-c24af96fbdca'),
  digest: jest.fn(async () => new ArrayBuffer(32)),
}));

describe('TelemetryDeck product analytics', () => {
  beforeEach(async () => {
    await AsyncStorage.clear();
    resetAnalyticsConsentForTests();
    await updateAnalyticsConsent(true);
    resetAnalyticsForTests(productionConfig);
    mockSignal.mockClear();
    (createTelemetryDeck as jest.Mock).mockClear();
    (Crypto.randomUUID as jest.Mock).mockClear();
  });

  it('uses a separate persisted installation id and production mode', async () => {
    await trackDailyActive(new Date('2026-09-08T01:00:00Z'));
    await trackSectionViewed('news');

    expect(createTelemetryDeck).toHaveBeenCalledTimes(1);
    expect(createTelemetryDeck).toHaveBeenCalledWith(
      expect.objectContaining({
        appID: 'telemetry-app-id',
        clientUser: '67c239a7-77be-4e3b-bac4-c24af96fbdca',
        testMode: false,
      }),
    );
    expect(await AsyncStorage.getItem('queenzone.mobile.analyticsInstallationId')).toBe(
      '67c239a7-77be-4e3b-bac4-c24af96fbdca',
    );
  });

  it('sends daily active once per day and no account or content data', async () => {
    await Promise.all([
      trackDailyActive(new Date('2026-09-08T01:00:00Z')),
      trackDailyActive(new Date('2026-09-08T23:00:00Z')),
    ]);
    await trackDailyActive(new Date('2026-09-09T01:00:00Z'));

    expect(mockSignal).toHaveBeenCalledTimes(2);
    expect(mockSignal.mock.calls[0]).toEqual([
      'app.active',
      expect.objectContaining({
        'TelemetryDeck.AppInfo.version': '1.2.3',
        'TelemetryDeck.AppInfo.buildNumber': '42',
        'TelemetryDeck.RunContext.targetEnvironment': 'native',
      }),
    ]);
    expect(JSON.stringify(mockSignal.mock.calls)).not.toMatch(
      /member|email|token|content|route|url/i,
    );
  });

  it('counts changes between allowlisted sections but suppresses repeats', async () => {
    await trackSectionViewed('home');
    await trackSectionViewed('home');
    await trackSectionViewed(null);
    await trackSectionViewed('archive');

    expect(mockSignal).toHaveBeenCalledTimes(2);
    expect(mockSignal.mock.calls).toEqual([
      ['section.viewed', expect.objectContaining({ section: 'home' })],
      ['section.viewed', expect.objectContaining({ section: 'archive' })],
    ]);
  });

  it('swallows analytics delivery failures', async () => {
    mockSignal.mockRejectedValueOnce(new Error('offline'));
    await expect(trackSectionViewed('forum')).resolves.toBeUndefined();
  });

  it('stays disabled without an App ID', async () => {
    resetAnalyticsForTests({ ...productionConfig, telemetryDeckAppId: undefined });

    await trackDailyActive(new Date('2026-09-08T01:00:00Z'));
    await trackSectionViewed('home');

    expect(createTelemetryDeck).not.toHaveBeenCalled();
    expect(Crypto.randomUUID).not.toHaveBeenCalled();
    expect(mockSignal).not.toHaveBeenCalled();
  });

  it('does not create an identity or send before consent', async () => {
    await AsyncStorage.clear();
    resetAnalyticsConsentForTests();
    resetAnalyticsForTests(productionConfig);

    await trackDailyActive(new Date('2026-09-08T01:00:00Z'));
    await trackSectionViewed('home');

    expect(createTelemetryDeck).not.toHaveBeenCalled();
    expect(Crypto.randomUUID).not.toHaveBeenCalled();
    expect(mockSignal).not.toHaveBeenCalled();
  });

  it('withdraws consent and removes the analytics-only device state', async () => {
    await trackDailyActive(new Date('2026-09-08T01:00:00Z'));
    expect(await AsyncStorage.getItem('queenzone.mobile.analyticsInstallationId')).not.toBeNull();

    await updateAnalyticsConsent(false);
    await trackSectionViewed('news');

    expect(await AsyncStorage.getItem('queenzone.mobile.analyticsInstallationId')).toBeNull();
    expect(await AsyncStorage.getItem('queenzone.mobile.analyticsLastActiveDay')).toBeNull();
    expect(mockSignal).toHaveBeenCalledTimes(1);
  });

  it('marks configured non-production builds as test data', async () => {
    resetAnalyticsForTests({ ...productionConfig, appEnv: 'staging' });

    await trackSectionViewed('photography');

    expect(createTelemetryDeck).toHaveBeenCalledWith(
      expect.objectContaining({ testMode: true }),
    );
  });
});
