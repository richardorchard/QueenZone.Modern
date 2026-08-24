import { NavigationContainer } from '@react-navigation/native';
import { render, type RenderOptions } from '@testing-library/react-native';
import type { ReactElement } from 'react';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { ThemeProvider } from '../theme';

const safeAreaMetrics = {
  frame: { x: 0, y: 0, width: 390, height: 844 },
  insets: { top: 47, left: 0, right: 0, bottom: 34 },
};

type Options = RenderOptions & {
  navigation?: boolean;
};

export function renderWithProviders(ui: ReactElement, options: Options = {}) {
  const { navigation = true, ...renderOptions } = options;
  const content = navigation ? <NavigationContainer>{ui}</NavigationContainer> : ui;

  return render(
    <SafeAreaProvider initialMetrics={safeAreaMetrics}>
      <ThemeProvider>{content}</ThemeProvider>
    </SafeAreaProvider>,
    renderOptions,
  );
}

export function fakeNavigation() {
  return {
    navigate: jest.fn(),
    goBack: jest.fn(),
    setOptions: jest.fn(),
    addListener: jest.fn(() => jest.fn()),
    isFocused: jest.fn(() => true),
    dispatch: jest.fn(),
    reset: jest.fn(),
    canGoBack: jest.fn(() => false),
    getParent: jest.fn(),
    getState: jest.fn(),
    getId: jest.fn(),
  };
}
