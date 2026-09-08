import AsyncStorage from '@react-native-async-storage/async-storage';
import {
  isAnalyticsConsentGranted,
  loadAnalyticsConsent,
  persistAnalyticsConsent,
  resetAnalyticsConsentForTests,
} from './consent';

describe('analytics consent storage', () => {
  beforeEach(async () => {
    await AsyncStorage.clear();
    resetAnalyticsConsentForTests();
  });

  it('fails closed when no choice has been made', async () => {
    await expect(loadAnalyticsConsent()).resolves.toBe('unset');
    await expect(isAnalyticsConsentGranted()).resolves.toBe(false);
  });

  it('persists grant and refusal separately from account settings', async () => {
    await persistAnalyticsConsent(true);
    expect(await AsyncStorage.getItem('queenzone.mobile.analyticsConsent')).toBe('granted');
    await expect(isAnalyticsConsentGranted()).resolves.toBe(true);

    await persistAnalyticsConsent(false);
    expect(await AsyncStorage.getItem('queenzone.mobile.analyticsConsent')).toBe('denied');
    await expect(isAnalyticsConsentGranted()).resolves.toBe(false);
  });
});
