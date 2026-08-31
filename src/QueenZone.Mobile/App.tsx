import {
  DarkTheme,
  DefaultTheme,
  NavigationContainer,
  useNavigationContainerRef,
} from '@react-navigation/native';
import * as SplashScreen from 'expo-splash-screen';
import { StatusBar } from 'expo-status-bar';
import { useEffect, useReducer } from 'react';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { RootNavigator } from './src/navigation/RootNavigator';
import { configureForegroundNotificationHandler } from './src/notifications';
import { SessionProvider } from './src/session/SessionContext';
import { FanPerformancePlayerProvider } from './src/audio/FanPerformancePlayer';
import { navigationIntegration } from './src/config/sentry';
import { BootSplash } from './src/splash/BootSplash';
import {
  SPLASH_FADE_MS,
  SPLASH_MAX_WAIT_MS,
  SPLASH_MIN_VISIBLE_MS,
  bootSplashReducer,
  initialBootSplashState,
} from './src/splash/bootSplashMachine';
import { ThemeProvider, dark, useQueenzoneFonts, useTheme } from './src/theme';

SplashScreen.preventAutoHideAsync().catch(() => {
  /* already prevented or unavailable in tests */
});

configureForegroundNotificationHandler();

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
  const [splash, dispatch] = useReducer(bootSplashReducer, initialBootSplashState);

  useEffect(() => {
    if (appReady) {
      SplashScreen.hideAsync().catch(() => {
        /* splash already hidden */
      });
    }
  }, [appReady]);

  useEffect(() => {
    if (appReady) {
      dispatch({ type: 'ASSETS_READY' });
    }
  }, [appReady]);

  useEffect(() => {
    const floor = setTimeout(() => dispatch({ type: 'FLOOR_ELAPSED' }), SPLASH_MIN_VISIBLE_MS);
    const ceiling = setTimeout(() => dispatch({ type: 'CEILING_REACHED' }), SPLASH_MAX_WAIT_MS);
    return () => {
      clearTimeout(floor);
      clearTimeout(ceiling);
    };
  }, []);

  useEffect(() => {
    if (splash.phase !== 'fading') return undefined;
    const fade = setTimeout(() => dispatch({ type: 'FADE_COMPLETE' }), SPLASH_FADE_MS);
    return () => clearTimeout(fade);
  }, [splash.phase]);

  if (splash.phase === 'booting') {
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
      {splash.phase !== 'done' && (
        <BootSplash fontsReady={Boolean(fontsLoaded)} fadingOut={splash.phase === 'fading'} />
      )}
    </GestureHandlerRootView>
  );
}

/** Dark-first page colour for any pre-provider splash / native chrome. */
export const bootstrapBackground = dark.surfacePage;
