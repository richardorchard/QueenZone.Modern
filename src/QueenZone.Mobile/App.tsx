import {
  DarkTheme,
  DefaultTheme,
  NavigationContainer,
  useNavigationContainerRef,
} from '@react-navigation/native';
import * as SplashScreen from 'expo-splash-screen';
import { StatusBar } from 'expo-status-bar';
import { useEffect, useRef, useState } from 'react';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { RootNavigator } from './src/navigation/RootNavigator';
import { SessionProvider } from './src/session/SessionContext';
import { FanPerformancePlayerProvider } from './src/audio/FanPerformancePlayer';
import { navigationIntegration } from './src/config/sentry';
import { BootSplash } from './src/splash/BootSplash';
import { ThemeProvider, dark, useQueenzoneFonts, useTheme } from './src/theme';

SplashScreen.preventAutoHideAsync().catch(() => {
  /* already prevented or unavailable in tests */
});

/** In-app splash floor/ceiling (design handoff): never flash, never block. */
const SPLASH_MIN_VISIBLE_MS = 600;
const SPLASH_MAX_WAIT_MS = 2500;
const SPLASH_FADE_MS = 320;

function AppNavigation() {
  const { c, mode } = useTheme();
  const base = mode === 'light' ? DefaultTheme : DarkTheme;

  const navigationTheme = {
    ...base,
    colors: {
      ...base.colors,
      primary: c.accentPrimary,
      background: c.surfacePage,
      card: c.surfacePage,
      text: c.textPrimary,
      border: c.hairline,
      notification: c.accentPrimary,
    },
  };

  const navigationRef = useNavigationContainerRef();

  return (
    <NavigationContainer
      ref={navigationRef}
      theme={navigationTheme}
      onReady={() => navigationIntegration.registerNavigationContainer(navigationRef)}
    >
      <StatusBar style={mode === 'light' ? 'dark' : 'light'} />
      <RootNavigator />
    </NavigationContainer>
  );
}

export default function App() {
  const [fontsLoaded, fontError] = useQueenzoneFonts();
  const appReady = fontsLoaded || fontError;
  const mountedAtRef = useRef(Date.now());
  const [splashVisible, setSplashVisible] = useState(true);
  const [fadingOut, setFadingOut] = useState(false);

  useEffect(() => {
    if (appReady) {
      SplashScreen.hideAsync().catch(() => {
        /* splash already hidden */
      });
    }
  }, [appReady]);

  // Minimum on-screen time so the splash never flickers past.
  useEffect(() => {
    if (!appReady) return undefined;
    const elapsed = Date.now() - mountedAtRef.current;
    const timer = setTimeout(() => setFadingOut(true), Math.max(0, SPLASH_MIN_VISIBLE_MS - elapsed));
    return () => clearTimeout(timer);
  }, [appReady]);

  // Hard ceiling — show the app shell regardless of boot state past this point.
  useEffect(() => {
    const timer = setTimeout(() => setFadingOut(true), SPLASH_MAX_WAIT_MS);
    return () => clearTimeout(timer);
  }, []);

  useEffect(() => {
    if (!fadingOut) return undefined;
    const timer = setTimeout(() => setSplashVisible(false), SPLASH_FADE_MS);
    return () => clearTimeout(timer);
  }, [fadingOut]);

  if (!appReady && !fadingOut) {
    return <BootSplash fontsReady={false} fadingOut={false} />;
  }

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <SafeAreaProvider>
        <ThemeProvider preference="dark">
          <SessionProvider>
            <FanPerformancePlayerProvider>
              <AppNavigation />
            </FanPerformancePlayerProvider>
          </SessionProvider>
        </ThemeProvider>
      </SafeAreaProvider>
      {splashVisible && <BootSplash fontsReady={Boolean(fontsLoaded)} fadingOut={fadingOut} />}
    </GestureHandlerRootView>
  );
}

/** Dark-first page colour for any pre-provider splash / native chrome. */
export const bootstrapBackground = dark.surfacePage;
