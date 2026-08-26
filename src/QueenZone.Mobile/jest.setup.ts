import { cleanup } from '@testing-library/react-native';

jest.mock('@react-native-async-storage/async-storage', () =>
  require('@react-native-async-storage/async-storage/jest/async-storage-mock'),
);

jest.mock('@sentry/react-native', () => ({
  init: jest.fn(),
  captureException: jest.fn(),
  wrap: (app: unknown) => app,
  reactNavigationIntegration: jest.fn(() => ({
    registerNavigationContainer: jest.fn(),
  })),
}));

jest.mock('expo-font', () => ({
  useFonts: () => [true, null],
  isLoaded: () => true,
  loadAsync: jest.fn(),
}));

jest.mock('expo-splash-screen', () => ({
  preventAutoHideAsync: jest.fn(async () => {}),
  hideAsync: jest.fn(async () => {}),
}));

jest.mock('react-native-reanimated', () => require('./jest.reanimated.mock.js'));

jest.mock('react-native-gesture-handler', () => require('./jest.gesture-handler.mock.js'));

jest.mock('expo-web-browser', () => ({
  maybeCompleteAuthSession: jest.fn(),
  openAuthSessionAsync: jest.fn(),
  openBrowserAsync: jest.fn(async () => ({ type: 'dismiss' })),
  dismissAuthSession: jest.fn(),
  dismissBrowser: jest.fn(),
  warmUpAsync: jest.fn(async () => {}),
  coolDownAsync: jest.fn(async () => {}),
}));

jest.mock('expo-notifications', () => ({
  AndroidImportance: { DEFAULT: 3 },
  IosAuthorizationStatus: { NOT_DETERMINED: 0, DENIED: 1, AUTHORIZED: 2, PROVISIONAL: 3, EPHEMERAL: 4 },
  DEFAULT_ACTION_IDENTIFIER: 'expo.modules.notifications.actions.DEFAULT',
  getPermissionsAsync: jest.fn(async () => ({ granted: false, canAskAgain: true, status: 'undetermined' })),
  requestPermissionsAsync: jest.fn(async () => ({ granted: false, canAskAgain: true, status: 'undetermined' })),
  getDevicePushTokenAsync: jest.fn(async () => ({ type: 'ios', data: 'mock-device-token' })),
  setNotificationChannelAsync: jest.fn(async () => null),
  setNotificationHandler: jest.fn(),
  addPushTokenListener: jest.fn(() => ({ remove: jest.fn() })),
  addNotificationReceivedListener: jest.fn(() => ({ remove: jest.fn() })),
  addNotificationResponseReceivedListener: jest.fn(() => ({ remove: jest.fn() })),
  getLastNotificationResponseAsync: jest.fn(async () => null),
  clearLastNotificationResponseAsync: jest.fn(async () => {}),
}));

jest.mock('expo-image', () => {
  const { View } = require('react-native');
  return { Image: View };
});

jest.mock('lucide-react-native', () => {
  const React = require('react');
  const { View } = require('react-native');
  const Icon = () => React.createElement(View);
  return new Proxy(
    {},
    {
      get: () => Icon,
    },
  );
});

const originalError = console.error.bind(console);
const originalWarn = console.warn.bind(console);

const ignoredErrors = [/Require cycle:/, /was not wrapped in act\(/];

console.error = (...args: unknown[]) => {
  const text = args.map(String).join(' ');
  if (ignoredErrors.some((pattern) => pattern.test(text))) {
    if (/was not wrapped in act\(/.test(text)) {
      throw new Error(`Unexpected act warning:\n${text}`);
    }
    return;
  }
  originalError(...args);
};

console.warn = (...args: unknown[]) => {
  const text = args.map(String).join(' ');
  if (/was not wrapped in act\(/.test(text)) {
    throw new Error(`Unexpected act warning:\n${text}`);
  }
  originalWarn(...args);
};

afterEach(() => {
  cleanup();
});

const onUnhandled = (reason: unknown) => {
  throw reason instanceof Error ? reason : new Error(String(reason));
};

process.on('unhandledRejection', onUnhandled);
afterAll(() => {
  process.off('unhandledRejection', onUnhandled);
});
